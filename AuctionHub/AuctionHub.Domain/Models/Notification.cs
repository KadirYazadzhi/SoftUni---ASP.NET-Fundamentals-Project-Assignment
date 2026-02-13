using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionHub.Domain.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;
    
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = null!;

    public string? Link { get; set; } // URL to redirect (e.g. /Auctions/Details/5)

    public bool IsRead { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
