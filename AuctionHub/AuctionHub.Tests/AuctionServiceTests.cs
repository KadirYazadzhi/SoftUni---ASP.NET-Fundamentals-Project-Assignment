using AuctionHub.Data;
using AuctionHub.Models;
using AuctionHub.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AuctionHub.Tests;

public class AuctionServiceTests
{
    private AuctionHubDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AuctionHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
            .Options;
        
        var context = new AuctionHubDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldPlaceBidSuccessfully_WhenValid()
    {
        // Arrange
        var context = GetDatabaseContext();
        var mockNotification = new Mock<INotificationService>();
        var service = new AuctionService(context, mockNotification.Object);

        var seller = new ApplicationUser { Id = "seller1", Email = "seller@test.com", UserName = "seller" };
        var bidder = new ApplicationUser { Id = "bidder1", Email = "bidder@test.com", UserName = "bidder", WalletBalance = 1000m };
        
        var auction = new Auction 
        { 
            Id = 1, 
            Title = "Test Item", 
            CurrentPrice = 100m, 
            StartPrice = 100m,
            MinIncrease = 10m,
            SellerId = seller.Id,
            IsActive = true,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        context.Users.AddRange(seller, bidder);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(1, "bidder1", 120m);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bid placed successfully.", result.Message);
        
        var updatedAuction = await context.Auctions.Include(a => a.Bids).FirstAsync(a => a.Id == 1);
        Assert.Equal(120m, updatedAuction.CurrentPrice);
        Assert.Single(updatedAuction.Bids);
        
        var updatedBidder = await context.Users.FindAsync("bidder1");
        Assert.Equal(880m, updatedBidder!.WalletBalance); // 1000 - 120
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenBidderIsSeller()
    {
        // Arrange
        var context = GetDatabaseContext();
        var mockNotification = new Mock<INotificationService>();
        var service = new AuctionService(context, mockNotification.Object);

        var seller = new ApplicationUser { Id = "seller1", Email = "seller@test.com", UserName = "seller", WalletBalance = 1000m };
        
        var auction = new Auction 
        { 
            Id = 1, 
            SellerId = "seller1",
            CurrentPrice = 100m,
            MinIncrease = 10m,
            IsActive = true,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        context.Users.Add(seller);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(1, "seller1", 150m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You cannot bid on your own auction.", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldRefundPreviousBidder()
    {
        // Arrange
        var context = GetDatabaseContext();
        var mockNotification = new Mock<INotificationService>();
        var service = new AuctionService(context, mockNotification.Object);

        var bidder1 = new ApplicationUser { Id = "bidder1", WalletBalance = 800m }; // Had 1000, bid 200
        var bidder2 = new ApplicationUser { Id = "bidder2", WalletBalance = 1000m };
        
        var auction = new Auction 
        { 
            Id = 1, 
            SellerId = "seller1",
            CurrentPrice = 200m,
            MinIncrease = 10m,
            IsActive = true,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        var initialBid = new Bid { AuctionId = 1, BidderId = "bidder1", Amount = 200m, BidTime = DateTime.UtcNow.AddHours(-1) };
        auction.Bids.Add(initialBid);

        context.Users.AddRange(bidder1, bidder2);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(1, "bidder2", 300m);

        // Assert
        Assert.True(result.Success);
        
        var updatedBidder1 = await context.Users.FindAsync("bidder1");
        Assert.Equal(1000m, updatedBidder1!.WalletBalance); // 800 + 200 refund
        
        var updatedBidder2 = await context.Users.FindAsync("bidder2");
        Assert.Equal(700m, updatedBidder2!.WalletBalance); // 1000 - 300 bid
    }

    [Fact]
    public async Task BuyItNowAsync_ShouldCloseAuction_AndRefundBidders()
    {
        // Arrange
        var context = GetDatabaseContext();
        var mockNotification = new Mock<INotificationService>();
        var service = new AuctionService(context, mockNotification.Object);

        var bidder1 = new ApplicationUser { Id = "bidder1", WalletBalance = 900m }; // Bid 100
        var buyer = new ApplicationUser { Id = "buyer", WalletBalance = 1000m };
        
        var auction = new Auction 
        { 
            Id = 1, 
            SellerId = "seller1",
            CurrentPrice = 100m,
            BuyItNowPrice = 500m,
            IsActive = true,
            EndTime = DateTime.UtcNow.AddDays(1)
        };
        
        auction.Bids.Add(new Bid { AuctionId = 1, BidderId = "bidder1", Amount = 100m });

        context.Users.AddRange(bidder1, buyer);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.BuyItNowAsync(1, "buyer");

        // Assert
        Assert.True(result.Success);
        
        var updatedAuction = await context.Auctions.FindAsync(1);
        Assert.False(updatedAuction!.IsActive); // Should be closed
        Assert.Equal(500m, updatedAuction.CurrentPrice);

        var updatedBidder1 = await context.Users.FindAsync("bidder1");
        Assert.Equal(1000m, updatedBidder1!.WalletBalance); // Refunded

        var updatedBuyer = await context.Users.FindAsync("buyer");
        Assert.Equal(500m, updatedBuyer!.WalletBalance); // Paid 500
    }
}
