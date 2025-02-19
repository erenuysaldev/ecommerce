using ECommerceProject.Core.Entities.Identity;

namespace ECommerceProject.Core.Entities
{
    public class SellerReview
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string UserId { get; set; }  // Değerlendiren kullanıcı
        public int Rating { get; set; }  // 1-5 arası
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }

        // Navigation properties
        public Seller Seller { get; set; }
        public ApplicationUser User { get; set; }
    }
} 