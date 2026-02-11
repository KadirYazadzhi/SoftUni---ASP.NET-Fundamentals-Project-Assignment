using System.ComponentModel.DataAnnotations;

namespace AuctionHub.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string IconClass { get; set; } = "bi-tag-fill"; // Default icon

    public virtual ICollection<Auction> Auctions { get; set; } = new HashSet<Auction>();
}
