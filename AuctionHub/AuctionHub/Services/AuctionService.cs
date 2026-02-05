using AuctionHub.Data;
using AuctionHub.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Services;

public class AuctionService : IAuctionService
{
    private readonly AuctionHubDbContext _context;

    public AuctionService(AuctionHubDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> PlaceBidAsync(int auctionId, string userId, decimal amount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null) return (false, "Auction not found.");

            // Validations
            if (auction.SellerId == userId) return (false, "You cannot bid on your own auction.");
            if (!auction.IsActive || auction.EndTime <= DateTime.UtcNow) return (false, "This auction has ended.");
            if (amount < auction.CurrentPrice + auction.MinIncrease) return (false, $"Bid must be at least {auction.CurrentPrice + auction.MinIncrease:C}.");
            if (auction.BuyItNowPrice.HasValue && amount >= auction.BuyItNowPrice.Value)
            {
                 // Usually we redirect to BuyItNow logic, but here let's just allow it as a normal bid
                 // or restrict it? Let's treat it as a bid.
            }

            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null || currentUser.WalletBalance < amount) return (false, "Insufficient funds.");

            // Money Logic
            currentUser.WalletBalance -= amount;

            var previousHighBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (previousHighBid != null)
            {
                if (previousHighBid.BidderId == userId)
                {
                    currentUser.WalletBalance += previousHighBid.Amount;
                }
                else
                {
                    var previousBidder = previousHighBid.Bidder;
                    previousBidder.WalletBalance += previousHighBid.Amount;
                }
            }

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderId = userId,
                Amount = amount,
                BidTime = DateTime.UtcNow
            };

            auction.CurrentPrice = amount;
            auction.Bids.Add(bid);

            // Check if bid meets BuyItNow price to auto-close? 
            // Optional: If bid >= BuyItNow, close it.
            if (auction.BuyItNowPrice.HasValue && amount >= auction.BuyItNowPrice.Value)
            {
                auction.IsActive = false;
                auction.EndTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Bid placed successfully.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (false, "Concurrency error: Someone else placed a bid. Please try again.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return (false, "An error occurred while placing bid.");
        }
    }

    public async Task<(bool Success, string Message)> BuyItNowAsync(int auctionId, string userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null) return (false, "Auction not found.");
            if (!auction.BuyItNowPrice.HasValue) return (false, "This auction does not support 'Buy It Now'.");
            
            decimal price = auction.BuyItNowPrice.Value;

            if (auction.SellerId == userId) return (false, "You cannot buy your own item.");
            if (!auction.IsActive || auction.EndTime <= DateTime.UtcNow) return (false, "Auction ended.");

            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null || currentUser.WalletBalance < price) return (false, "Insufficient funds.");

            // Money Logic
            currentUser.WalletBalance -= price;

            // Refund any previous bidder
            var previousHighBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (previousHighBid != null)
            {
                // Refund logic
                 if (previousHighBid.BidderId == userId)
                {
                     currentUser.WalletBalance += previousHighBid.Amount;
                }
                else
                {
                    var previousBidder = previousHighBid.Bidder;
                    previousBidder.WalletBalance += previousHighBid.Amount;
                }
            }

            // Create winning bid
            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderId = userId,
                Amount = price,
                BidTime = DateTime.UtcNow
            };

            auction.CurrentPrice = price;
            auction.Bids.Add(bid);
            
            // Close Auction
            auction.IsActive = false;
            auction.EndTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Congratulations! You have purchased this item.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return (false, "An error occurred during purchase.");
        }
    }
}
