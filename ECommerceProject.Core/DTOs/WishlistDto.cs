namespace ECommerceProject.Core.DTOs
{
    public class WishlistItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public DateTime AddedAt { get; set; }
        public string SellerStoreName { get; set; }
    }
} 