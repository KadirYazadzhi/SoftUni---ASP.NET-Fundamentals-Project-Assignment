using AuctionHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly IAuctionHubDbContext _context;
    private readonly INotificationService _notificationService;

    public DashboardController(IAuctionHubDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalAuctions = await _context.Auctions.CountAsync();
        var activeAuctions = await _context.Auctions.CountAsync(a => a.IsActive && a.EndTime > DateTime.UtcNow);
        var totalBids = await _context.Bids.CountAsync();
        var totalWalletBalance = await _context.Users.SumAsync(u => u.WalletBalance);

        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalAuctions = totalAuctions;
        ViewBag.ActiveAuctions = activeAuctions;
        ViewBag.TotalBids = totalBids;
        ViewBag.TotalWalletBalance = totalWalletBalance;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendAnnouncement(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Message cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        await _notificationService.NotifyAllUsersAsync($"📢 SYSTEM: {message}");
        TempData["Success"] = "Announcement sent to all users.";
        return RedirectToAction(nameof(Index));
    }
}
