using AuctionHub.Application.Interfaces;
using AuctionHub.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    private readonly IAuctionHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(IAuctionHubDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => (u.Email != null && u.Email.Contains(searchTerm)) || 
                                     (u.UserName != null && u.UserName.Contains(searchTerm)) || 
                                     (u.FirstName != null && u.FirstName.Contains(searchTerm)) || 
                                     (u.LastName != null && u.LastName.Contains(searchTerm)));
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
        // Simple transaction is good, but for full concurrency safety we should rely on RowVersion
        // However, EF Core handles concurrency via DbUpdateConcurrencyException automatically if RowVersion is present in the model.
        // We just need to catch it, which we already do.
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            user.WalletBalance += amount;
            
            // Force update of RowVersion
            _context.Entry(user).Property(u => u.RowVersion).IsModified = true;
            
            _context.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                Amount = amount,
                TransactionType = amount >= 0 ? "AdminBonus" : "AdminPenalty",
                Description = string.IsNullOrEmpty(reason) ? "Administrative adjustment" : reason,
                TransactionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Balance updated. New balance: {user.WalletBalance:C}";
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Concurrency error: The user's balance was modified by another process. Please try again.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "An error occurred while updating the balance.";
        }

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
