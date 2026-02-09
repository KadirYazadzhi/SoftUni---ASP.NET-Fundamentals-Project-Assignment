using AuctionHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Areas.Admin.Controllers;

public class TransactionsController : AdminBaseController
{
    private readonly AuctionHubDbContext _context;

    public TransactionsController(AuctionHubDbContext context)
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
