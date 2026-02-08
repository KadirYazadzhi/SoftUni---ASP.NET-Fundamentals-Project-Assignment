using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionHub.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WalletBalance { get; set; } = 0.00m;

    [NotMapped]
    public string DisplayName 
    {
        get 
        {
            if (!string.IsNullOrEmpty(UserName)) return UserName;
            if (string.IsNullOrEmpty(Email)) return "Unknown";
            return Email.Split('@')[0];
        }
    }

    public virtual ICollection<Auction> MyAuctions { get; set; } = new HashSet<Auction>();
    public virtual ICollection<Bid> MyBids { get; set; } = new HashSet<Bid>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();
    public virtual ICollection<AuctionWatchlist> Watchlist { get; set; } = new HashSet<AuctionWatchlist>();
}
