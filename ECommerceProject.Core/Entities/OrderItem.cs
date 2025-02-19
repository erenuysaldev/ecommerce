namespace ECommerceProject.Core.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int SellerId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; }  // Pending, Accepted, Rejected, Shipped, Delivered

        // Navigation properties
        public Order Order { get; set; }
        public Product Product { get; set; }
        public Seller Seller { get; set; }
    }
} 