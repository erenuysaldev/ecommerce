namespace ECommerceProject.Core.DTOs
{
    public class SellerDto
    {
        public int Id { get; set; }
        public string StoreName { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
        public decimal Rating { get; set; }
        public int TotalSales { get; set; }
    }

    public class CreateSellerDto
    {
        public string StoreName { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
    }

    public class UpdateSellerDto
    {
        public string StoreName { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
    }
} 