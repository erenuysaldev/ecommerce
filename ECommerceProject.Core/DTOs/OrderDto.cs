namespace ECommerceProject.Core.DTOs
{
    public class CreateOrderDto
    {
        public List<OrderItemDto> Items { get; set; }
        public string ShippingAddress { get; set; }
        public string ContactPhone { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderDetailsDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string ContactPhone { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public List<OrderItemDetailsDto> Items { get; set; }
    }

    public class OrderItemDetailsDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; }
        public string SellerStoreName { get; set; }
    }

    public class UpdateOrderItemStatusDto
    {
        public string Status { get; set; }  // Accepted, Rejected, Shipped, Delivered
    }
} 