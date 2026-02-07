namespace AuctionHub.Services;

public interface INotificationService
{
    Task NotifyUserAsync(string userId, string message, string? link = null);
    Task NotifyAllWatchersAsync(int auctionId, string message, string? link = null, string? excludeUserId = null);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}
