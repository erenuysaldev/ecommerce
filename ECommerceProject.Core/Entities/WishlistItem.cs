using ECommerceProject.Core.Entities.Identity;

namespace ECommerceProject.Core.Entities
{
    public class WishlistItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime AddedAt { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; }
        public Product Product { get; set; }
    }
} 