using AuctionHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Areas.Admin.Controllers;

public class TransactionsController : AdminBaseController
{
    private readonly IAuctionHubDbContext _context;

    public TransactionsController(IAuctionHubDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var transactions = await _context.Transactions
            .Include(t => t.User)
            .OrderByDescending(t => t.TransactionDate)
            .Take(100) // Limit for performance
            .ToListAsync();

        return View(transactions);
    }
}
