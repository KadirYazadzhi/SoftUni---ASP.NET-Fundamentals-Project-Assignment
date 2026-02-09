using AuctionHub.Data;
using AuctionHub.Models;
using AuctionHub.Services;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Services;

public class AuctionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuctionCleanupService> _logger;

    public AuctionCleanupService(IServiceProvider serviceProvider, ILogger<AuctionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Auction Cleanup Service running at: {time}", DateTimeOffset.Now);

            await CloseExpiredAuctionsAsync();

            // Run every 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CloseExpiredAuctionsAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuctionHubDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Find active auctions that have passed their EndTime
            var expiredAuctions = await context.Auctions
                .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
                .Where(a => a.IsActive && a.EndTime <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredAuctions.Any())
            {
                foreach (var auction in expiredAuctions)
                {
                    auction.IsActive = false;
                    _logger.LogInformation($"Closing auction {auction.Id}: {auction.Title}");

                    var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();

                    if (winningBid != null)
                    {
                        // Notify Winner
                        await notificationService.NotifyUserAsync(winningBid.BidderId, 
                            $"🎉 Congratulations! You won the auction for '{auction.Title}' with a bid of {winningBid.Amount:C}!", 
                            $"/Auctions/Details/{auction.Id}");

                        // Notify Seller
                        await notificationService.NotifyUserAsync(auction.SellerId, 
                            $"💰 Your item '{auction.Title}' was sold to {winningBid.Bidder.DisplayName} for {winningBid.Amount:C}!", 
                            $"/Auctions/Details/{auction.Id}");
                    }
                    else
                    {
                        // Notify Seller - No bids
                        await notificationService.NotifyUserAsync(auction.SellerId, 
                            $"📉 Your auction for '{auction.Title}' has ended with no bids.", 
                            $"/Auctions/Details/{auction.Id}");
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}