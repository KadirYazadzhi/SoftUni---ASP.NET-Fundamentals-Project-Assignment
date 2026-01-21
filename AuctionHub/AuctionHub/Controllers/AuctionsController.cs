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
