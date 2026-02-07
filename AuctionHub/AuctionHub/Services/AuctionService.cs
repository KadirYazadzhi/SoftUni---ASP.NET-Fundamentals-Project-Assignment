using AuctionHub.Data;
using AuctionHub.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Services;

public class AuctionService : IAuctionService
{
    private readonly AuctionHubDbContext _context;
    private readonly INotificationService _notificationService;

    public AuctionService(AuctionHubDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, string Message)> PlaceBidAsync(int auctionId, string userId, decimal amount)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

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

            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null || currentUser.WalletBalance < amount) return (false, "Insufficient funds.");

            // 1. Charge User
            currentUser.WalletBalance -= amount;
            _context.Transactions.Add(new Transaction
            {
                UserId = userId,
                Amount = amount,
                Description = $"Bid on '{auction.Title}'",
                TransactionType = "Bid",
                TransactionDate = DateTime.UtcNow
            });

            // 2. Refund Previous Bidder & Notify
            var previousHighBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (previousHighBid != null)
            {
                if (previousHighBid.BidderId == userId)
                {
                    currentUser.WalletBalance += previousHighBid.Amount;
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = userId,
                        Amount = previousHighBid.Amount,
                        Description = $"Refund outbid on '{auction.Title}'",
                        TransactionType = "Refund",
                        TransactionDate = DateTime.UtcNow
                    });
                }
                else
                {
                    var previousBidder = previousHighBid.Bidder;
                    previousBidder.WalletBalance += previousHighBid.Amount;
                     _context.Transactions.Add(new Transaction
                    {
                        UserId = previousBidder.Id,
                        Amount = previousHighBid.Amount,
                        Description = $"Refund outbid on '{auction.Title}'",
                        TransactionType = "Refund",
                        TransactionDate = DateTime.UtcNow
                    });

                    // NOTIFY PREVIOUS BIDDER
                    await _notificationService.NotifyUserAsync(previousBidder.Id, 
                        $"You have been outbid on '{auction.Title}'! Current price: {amount:C}", 
                        $"/Auctions/Details/{auctionId}");
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

            // NOTIFY WATCHERS
            await _notificationService.NotifyAllWatchersAsync(auctionId, 
                $"New bid on watched item '{auction.Title}': {amount:C}", 
                $"/Auctions/Details/{auctionId}",
                excludeUserId: userId);

            if (auction.BuyItNowPrice.HasValue && amount >= auction.BuyItNowPrice.Value)
            {
                auction.IsActive = false;
                auction.EndTime = DateTime.UtcNow;
                
                // NOTIFY EVERYONE IT'S SOLD
                await _notificationService.NotifyAllWatchersAsync(auctionId, 
                    $"Auction '{auction.Title}' has ended (Buy It Now price reached).", 
                    $"/Auctions/Details/{auctionId}");
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Bid placed successfully.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return (false, "Concurrency error: Someone else placed a bid. Please try again.");
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            return (false, "An error occurred while placing bid.");
        }
    }

    public async Task<(bool Success, string Message)> BuyItNowAsync(int auctionId, string userId)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

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

            // 1. Charge User
            currentUser.WalletBalance -= price;
            _context.Transactions.Add(new Transaction
            {
                UserId = userId,
                Amount = price,
                Description = $"Purchased '{auction.Title}' (Buy It Now)",
                TransactionType = "Purchase",
                TransactionDate = DateTime.UtcNow
            });

            // 2. Refund Previous Bidder
            var previousHighBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (previousHighBid != null)
            {
                 if (previousHighBid.BidderId == userId)
                {
                     currentUser.WalletBalance += previousHighBid.Amount;
                     _context.Transactions.Add(new Transaction
                    {
                        UserId = userId,
                        Amount = previousHighBid.Amount,
                        Description = $"Refund (BIN upgrade) on '{auction.Title}'",
                        TransactionType = "Refund",
                        TransactionDate = DateTime.UtcNow
                    });
                }
                else
                {
                    var previousBidder = previousHighBid.Bidder;
                    previousBidder.WalletBalance += previousHighBid.Amount;
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = previousBidder.Id,
                        Amount = previousHighBid.Amount,
                        Description = $"Refund (Item Sold) on '{auction.Title}'",
                        TransactionType = "Refund",
                        TransactionDate = DateTime.UtcNow
                    });

                     // NOTIFY PREVIOUS BIDDER
                    await _notificationService.NotifyUserAsync(previousBidder.Id, 
                        $"Item '{auction.Title}' was purchased via Buy It Now. Your bid has been refunded.", 
                        $"/Auctions/Details/{auctionId}");
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

            // NOTIFY WATCHERS
            await _notificationService.NotifyAllWatchersAsync(auctionId, 
                $"Item '{auction.Title}' was sold for {price:C}!", 
                $"/Auctions/Details/{auctionId}",
                excludeUserId: userId);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Congratulations! You have purchased this item.");
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            return (false, "An error occurred during purchase.");
        }
    }
}
