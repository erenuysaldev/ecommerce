using ECommerceProject.Core.Entities.Identity;

namespace ECommerceProject.Core.Entities
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<CartItem> Items { get; set; }
    }
} 