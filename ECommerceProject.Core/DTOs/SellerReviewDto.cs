namespace ECommerceProject.Core.DTOs
{
    public class CreateSellerReviewDto
    {
        public int SellerId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class SellerReviewDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 