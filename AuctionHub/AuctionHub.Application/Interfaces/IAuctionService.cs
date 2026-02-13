using AuctionHub.Domain.Models;
using AuctionHub.Application.Interfaces;
namespace AuctionHub.Application.Interfaces;

public interface IAuctionService
{
    Task<(bool Success, string Message)> PlaceBidAsync(int auctionId, string userId, decimal amount);
    Task<(bool Success, string Message)> BuyItNowAsync(int auctionId, string userId);
}
