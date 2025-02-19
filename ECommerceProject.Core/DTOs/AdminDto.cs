namespace ECommerceProject.Core.DTOs
{
    public class ApproveReviewDto
    {
        public bool IsApproved { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingSellers { get; set; }
        public int PendingReviews { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RecentOrderDto> RecentOrders { get; set; }
    }

    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
} 