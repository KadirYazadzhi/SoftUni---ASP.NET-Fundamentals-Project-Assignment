using AuctionHub.Data;
using AuctionHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    private readonly AuctionHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(AuctionHubDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.Email.Contains(searchTerm) || u.UserName.Contains(searchTerm) || u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm));
        }

        var users = await query.ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _context.Users
            .Include(u => u.MyAuctions).ThenInclude(a => a.Category)
            .Include(u => u.MyBids).ThenInclude(b => b.Auction)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBalance(string userId, decimal amount, string reason)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.WalletBalance += amount;
        
        _context.Transactions.Add(new Transaction
        {
            UserId = user.Id,
            Amount = amount,
            TransactionType = amount >= 0 ? "AdminBonus" : "AdminPenalty",
            Description = string.IsNullOrEmpty(reason) ? "Administrative adjustment" : reason,
            TransactionDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Balance updated. New balance: {user.WalletBalance:C}";
        return RedirectToAction(nameof(Details), new { id = userId });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLock(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = "User unlocked successfully.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            TempData["Success"] = "User locked indefinitely.";
        }

        return RedirectToAction(nameof(Index));
    }
}
