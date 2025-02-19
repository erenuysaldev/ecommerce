using ECommerceProject.Core.Entities.Identity;

namespace ECommerceProject.Core.Entities
{
    public class Seller
    {
        public int Id { get; set; }
        public string UserId { get; set; }  // ApplicationUser ile ilişki için
        public string StoreName { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
        public decimal Rating { get; set; }
        public int TotalSales { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<Product> Products { get; set; }
    }
} 