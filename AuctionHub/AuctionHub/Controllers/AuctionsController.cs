using System.Security.Claims;
using AuctionHub.Data;
using AuctionHub.Models;
using AuctionHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AuctionHub.Controllers;

[Authorize]
public class AuctionsController : Controller
{
    private readonly AuctionHubDbContext _context;

    public AuctionsController(AuctionHubDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var auctions = await _context.Auctions
            .Include(a => a.Category)
            .Where(a => a.IsActive && a.EndTime > DateTime.Now)
            .OrderBy(a => a.EndTime)
            .Select(a => new AuctionListViewModel
            {
                Id = a.Id,
                Title = a.Title,
                ImageUrl = a.ImageUrl,
                CurrentPrice = a.CurrentPrice,
                EndTime = a.EndTime,
                Category = a.Category.Name,
                IsActive = a.IsActive
            })
            .ToListAsync();

        return View(auctions);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var auction = await _context.Auctions
            .Include(a => a.Category)
            .Include(a => a.Seller)
            .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction == null)
        {
            return NotFound();
        }

        var model = new AuctionDetailsViewModel
        {
            Id = auction.Id,
            Title = auction.Title,
            Description = auction.Description,
            ImageUrl = auction.ImageUrl,
            CurrentPrice = auction.CurrentPrice,
            StartPrice = auction.StartPrice,
            EndTime = auction.EndTime,
            Category = auction.Category.Name,
            Seller = auction.Seller.UserName ?? auction.Seller.Email ?? "Unknown",
            SellerId = auction.SellerId,
            IsActive = auction.IsActive && auction.EndTime > DateTime.Now,
            Bids = auction.Bids
                .OrderByDescending(b => b.BidTime)
                .Select(b => new BidViewModel
                {
                    Amount = b.Amount,
                    BidTime = b.BidTime,
                    Bidder = b.Bidder.UserName ?? b.Bidder.Email ?? "Unknown"
                })
                .ToList(),
            NewBidAmount = auction.CurrentPrice + 1 // Suggest a bid
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AuctionFormModel
        {
            Categories = await GetCategoriesAsync(),
            EndTime = DateTime.Now.AddDays(7)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AuctionFormModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoriesAsync();
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var auction = new Auction
        {
            Title = model.Title,
            Description = model.Description,
            ImageUrl = model.ImageUrl,
            StartPrice = model.StartPrice,
            CurrentPrice = model.StartPrice, // Initially same as start price
            EndTime = model.EndTime,
            CreatedOn = DateTime.Now,
            IsActive = true,
            CategoryId = model.CategoryId,
            SellerId = currentUserId
        };

        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }

    private async Task<IEnumerable<SelectListItem>> GetCategoriesAsync()
    {
        return await _context.Categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }
}
