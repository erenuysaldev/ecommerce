namespace ECommerceProject.Core.DTOs
{
    public class SellerStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalSales { get; set; }
        public decimal Rating { get; set; }
        public bool IsApproved { get; set; }
        public string StoreName { get; set; }
        public DateTime JoinDate { get; set; }
    }
} 