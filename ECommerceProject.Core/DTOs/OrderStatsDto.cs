namespace ECommerceProject.Core.DTOs
{
    public class OrderStatsDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<OrderStatusCountDto> OrdersByStatus { get; set; }
        public List<DailyOrderStatsDto> DailyStats { get; set; }
        public List<PaymentMethodStatsDto> PaymentMethodStats { get; set; }
    }

    public class OrderStatusCountDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class DailyOrderStatsDto
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentMethodStatsDto
    {
        public string Method { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
} 