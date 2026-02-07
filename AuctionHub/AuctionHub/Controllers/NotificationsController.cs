using System.Security.Claims;
using AuctionHub.Data;
using AuctionHub.Models;
using AuctionHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly AuctionHubDbContext _context;
    private readonly INotificationService _notificationService;

    public NotificationsController(AuctionHubDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedOn)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _notificationService.MarkAsReadAsync(id, userId!);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _notificationService.MarkAllAsReadAsync(userId!);
        return RedirectToAction(nameof(Index));
    }
    
    // API endpoint for the bell icon badge
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Ok(0);
        
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }
}
