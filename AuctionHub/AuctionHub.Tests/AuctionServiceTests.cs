using AuctionHub.Application.Interfaces;
using AuctionHub.Application.Services;
using AuctionHub.Infrastructure.Data;
using AuctionHub.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AuctionHub.Application.DTOs;

namespace AuctionHub.Tests;

public class AuctionServiceTests
{
    private readonly Mock<INotificationService> _mockNotificationService;

    public AuctionServiceTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
    }

    private AuctionHubDbContext GetDatabaseContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuctionHubDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        var context = new AuctionHubDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static ApplicationUser CreateUser(string id, decimal wallet = 1000m) => new()
    {
        Id = id,
        UserName = $"{id}@test.com",
        Email = $"{id}@test.com",
        WalletBalance = wallet,
        RowVersion = new byte[8]
    };

    private static Category CreateCategory(int id = 1) => new() { Id = id, Name = "Test Category" };

    private static Auction CreateAuction(string sellerId, int categoryId, bool isActive = true, decimal currentPrice = 100m) => new()
    {
        Id = 1,
        Title = "Test Auction",
        Description = "Test Description",
        SellerId = sellerId,
        CategoryId = categoryId,
        IsActive = isActive,
        EndTime = isActive ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow.AddDays(-1),
        StartPrice = 100m,
        CurrentPrice = currentPrice,
        MinIncrease = 10m,
        BuyItNowPrice = 500m,
        RowVersion = new byte[8],
        CreatedOn = DateTime.UtcNow
    };

    [Fact]
    public async Task PlaceBidAsync_ShouldPlaceBidSuccessfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);

        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder.Id, 120m);

        // Assert
        Assert.True(result.Success);
        var updatedAuction = await context.Auctions.FindAsync(auction.Id);
        Assert.Equal(120m, updatedAuction!.CurrentPrice);
        var updatedBidder = await context.Users.FindAsync(bidder.Id);
        Assert.Equal(880m, updatedBidder!.WalletBalance);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenAuctionNotFound()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);
        var bidder = CreateUser("bidder");
        context.Users.Add(bidder);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(999, bidder.Id, 150m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Auction not found.", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenUserIsAdministrator()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var admin = CreateUser("admin");
        var seller = CreateUser("seller");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);
        var adminRole = new IdentityRole { Id = "admin_role", Name = "Administrator", NormalizedName = "ADMINISTRATOR" };

        context.Users.AddRange(admin, seller);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, admin.Id, 200m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Administrators are restricted from participating in auctions.", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenSellerBidsOnOwnAuction()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);

        context.Users.Add(seller);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, seller.Id, 150m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You cannot bid on your own auction.", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenAuctionIsInactive()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id, isActive: false);

        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder.Id, 150m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("This auction has ended.", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenBidIsTooLow()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id, currentPrice: 100m);
        auction.MinIncrease = 20m;

        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder.Id, 110m);

        // Assert
        Assert.False(result.Success);
        Assert.StartsWith("Bid must be at least", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldFail_WhenInsufficientFunds()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder", wallet: 50m);
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);

        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder.Id, 150m);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Insufficient funds.", result.Message);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldRefundPreviousBidder()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder1 = CreateUser("bidder1", wallet: 1000m);
        var bidder2 = CreateUser("bidder2", wallet: 1000m);
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id, currentPrice: 120m);
        var initialBid = new Bid { AuctionId = auction.Id, BidderId = bidder1.Id, Amount = 120m, Bidder = bidder1 };
        auction.Bids.Add(initialBid);

        context.Users.AddRange(seller, bidder1, bidder2);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();
        
        // Pre-check
        var bidder1Before = await context.Users.FindAsync(bidder1.Id);
        bidder1Before!.WalletBalance -= 120m; // Simulate initial bid deduction
        await context.SaveChangesAsync();
        Assert.Equal(880m, bidder1Before.WalletBalance);

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder2.Id, 150m);

        // Assert
        Assert.True(result.Success);
        var bidder1After = await context.Users.FindAsync(bidder1.Id);
        Assert.Equal(1000m, bidder1After!.WalletBalance); // Check refund
        var bidder2After = await context.Users.FindAsync(bidder2.Id);
        Assert.Equal(850m, bidder2After!.WalletBalance); // Check deduction
        _mockNotificationService.Verify(n => n.NotifyUserAsync(bidder1.Id, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public async Task PlaceBidAsync_ShouldHandleSelfOutbidCorrectly()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder", wallet: 1000m);
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id, currentPrice: 120m);
        var initialBid = new Bid { AuctionId = auction.Id, BidderId = bidder.Id, Amount = 120m, Bidder = bidder };
        auction.Bids.Add(initialBid);

        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();
        
        var bidderBefore = await context.Users.FindAsync(bidder.Id);
        bidderBefore!.WalletBalance -= 120m; // Simulate initial deduction
        await context.SaveChangesAsync();
        Assert.Equal(880m, bidderBefore.WalletBalance);

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder.Id, 150m);

        // Assert
        Assert.True(result.Success);
        var bidderAfter = await context.Users.FindAsync(bidder.Id);
        // Wallet should be 1000 (start) - 150 (final bid) = 850
        // The logic is: refund 120, then deduct 150.
        Assert.Equal(850m, bidderAfter!.WalletBalance);
    }

    [Fact]
    public async Task PlaceBidAsync_ShouldEndAuction_WhenBidMeetsBuyItNowPrice()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);
        auction.BuyItNowPrice = 200m;

        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PlaceBidAsync(auction.Id, bidder.Id, 200m);

        // Assert
        Assert.True(result.Success);
        var updatedAuction = await context.Auctions.FindAsync(auction.Id);
        Assert.False(updatedAuction!.IsActive);
        _mockNotificationService.Verify(n => n.NotifyAllWatchersAsync(auction.Id, It.Is<string>(s => s.Contains("has ended")), It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task BuyItNowAsync_ShouldSucceedAndCloseAuction()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var buyer = CreateUser("buyer", wallet: 1000m);
        var prevBidder = CreateUser("prevBidder", wallet: 1000m);
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id, currentPrice: 150m);
        auction.BuyItNowPrice = 500m;
        var initialBid = new Bid { AuctionId = auction.Id, BidderId = prevBidder.Id, Amount = 150m, Bidder = prevBidder };
        auction.Bids.Add(initialBid);

        context.Users.AddRange(seller, buyer, prevBidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();
        
        var prevBidderBefore = await context.Users.FindAsync(prevBidder.Id);
        prevBidderBefore!.WalletBalance -= 150m; // Simulate deduction
        await context.SaveChangesAsync();

        // Act
        var result = await service.BuyItNowAsync(auction.Id, buyer.Id);

        // Assert
        Assert.True(result.Success);
        var updatedAuction = await context.Auctions.FindAsync(auction.Id);
        Assert.False(updatedAuction!.IsActive);
        Assert.Equal(500m, updatedAuction.CurrentPrice);

        var buyerAfter = await context.Users.FindAsync(buyer.Id);
        Assert.Equal(500m, buyerAfter!.WalletBalance);

        var prevBidderAfter = await context.Users.FindAsync(prevBidder.Id);
        Assert.Equal(1000m, prevBidderAfter!.WalletBalance); // Check refund
        
        _mockNotificationService.Verify(n => n.NotifyAllWatchersAsync(auction.Id, It.Is<string>(s => s.Contains("was sold")), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task BuyItNowAsync_ShouldFail_WhenNoBuyItNowPrice()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var buyer = CreateUser("buyer");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);
        auction.BuyItNowPrice = null;

        context.Users.AddRange(seller, buyer);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.BuyItNowAsync(auction.Id, buyer.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("This auction does not support 'Buy It Now'.", result.Message);
    }

    [Fact]
    public async Task CreateAuctionAsync_ShouldCreateAuctionSuccessfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);
        var seller = CreateUser("seller");
        var category = CreateCategory();
        context.Users.Add(seller);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var auctionDto = new AuctionFormDto
        {
            Title = "New Shiny Bike",
            Description = "A very shiny bike.",
            StartPrice = 200m,
            MinIncrease = 10m,
            EndTime = DateTime.Now.AddDays(5),
            CategoryId = category.Id
        };

        // Act
        var auctionId = await service.CreateAuctionAsync(auctionDto, seller.Id);

        // Assert
        Assert.True(auctionId > 0);
        var auction = await context.Auctions.FindAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal("New Shiny Bike", auction.Title);
        Assert.Equal(200m, auction.StartPrice);
        Assert.Equal(200m, auction.CurrentPrice);
    }

    [Fact]
    public async Task CreateAuctionAsync_ShouldPreventDuplicates_WhenUsingIdempotencyKey()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);
        var seller = CreateUser("seller");
        var category = CreateCategory();
        context.Users.Add(seller);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var auctionDto = new AuctionFormDto
        {
            Title = "Duplicate Bike",
            Description = "A very shiny bike.",
            StartPrice = 200m,
            MinIncrease = 10m,
            EndTime = DateTime.Now.AddDays(5),
            CategoryId = category.Id
        };

        // Act
        var auctionId1 = await service.CreateAuctionAsync(auctionDto, seller.Id);
        var auctionId2 = await service.CreateAuctionAsync(auctionDto, seller.Id); // Immediate second call

        // Assert
        Assert.True(auctionId1 > 0);
        Assert.Equal(-1, auctionId2); // Second call should be rejected
        var auctionCount = await context.Auctions.CountAsync(a => a.Title == "Duplicate Bike");
        Assert.Equal(1, auctionCount);
    }

    [Fact]
    public async Task UpdateAuctionAsync_ShouldFail_WhenBidsExist()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);
        auction.Bids.Add(new Bid { BidderId = bidder.Id, Amount = 110m });
        
        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        var updateDto = new AuctionFormDto { Title = "Updated Title" };

        // Act
        var result = await service.UpdateAuctionAsync(auction.Id, updateDto, seller.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You cannot edit an auction that has existing bids.", result.Message);
    }

    [Fact]
    public async Task DeleteAuctionAsync_ShouldFail_WhenBidsExist()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var bidder = CreateUser("bidder");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);
        auction.Bids.Add(new Bid { BidderId = bidder.Id, Amount = 110m });
        
        context.Users.AddRange(seller, bidder);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAuctionAsync(auction.Id, seller.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cannot delete an auction that already has bids.", result.Message);
    }

    [Fact]
    public async Task ToggleWatchlistAsync_ShouldAddItemToWatchlist()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var watcher = CreateUser("watcher");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);

        context.Users.AddRange(seller, watcher);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ToggleWatchlistAsync(auction.Id, watcher.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Added to watchlist.", result.Message);
        var watchlistItem = await context.Watchlist.FirstOrDefaultAsync(w => w.AuctionId == auction.Id && w.UserId == watcher.Id);
        Assert.NotNull(watchlistItem);
    }

    [Fact]
    public async Task ToggleWatchlistAsync_ShouldRemoveItemFromWatchlist()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var seller = CreateUser("seller");
        var watcher = CreateUser("watcher");
        var category = CreateCategory();
        var auction = CreateAuction(seller.Id, category.Id);
        var watchlistItem = new AuctionWatchlist { AuctionId = auction.Id, UserId = watcher.Id };

        context.Users.AddRange(seller, watcher);
        context.Categories.Add(category);
        context.Auctions.Add(auction);
        context.Watchlist.Add(watchlistItem);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ToggleWatchlistAsync(auction.Id, watcher.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Removed from watchlist.", result.Message);
        var removedItem = await context.Watchlist.FirstOrDefaultAsync(w => w.AuctionId == auction.Id && w.UserId == watcher.Id);
        Assert.Null(removedItem);
    }
    
    [Fact]
    public async Task GetAuctionsAsync_ShouldHideAdminAuctions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = GetDatabaseContext(dbName);
        var service = new AuctionService(context, _mockNotificationService.Object);

        var admin = CreateUser("admin");
        var seller = CreateUser("seller");
        var category = CreateCategory();
        var adminRole = new IdentityRole { Id = "admin_role", Name = "Administrator", NormalizedName = "ADMINISTRATOR" };
        
        var publicAuction = CreateAuction(seller.Id, category.Id);
        publicAuction.Id = 1;
        var adminAuction = CreateAuction(admin.Id, category.Id);
        adminAuction.Id = 2;

        context.Users.AddRange(admin, seller);
        context.Categories.Add(category);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });
        context.Auctions.AddRange(publicAuction, adminAuction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAuctionsAsync(null, null, null, 1, 10, null, null, null);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(publicAuction.Id, result[0].Id);
    }
}
