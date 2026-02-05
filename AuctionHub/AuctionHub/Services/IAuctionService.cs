namespace AuctionHub.Services;

public interface IAuctionService
{
    Task<(bool Success, string Message)> PlaceBidAsync(int auctionId, string userId, decimal amount);
    Task<(bool Success, string Message)> BuyItNowAsync(int auctionId, string userId);
}
