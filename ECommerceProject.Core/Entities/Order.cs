using ECommerceProject.Core.Entities.Identity;

namespace ECommerceProject.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }  // Pending, Processing, Shipped, Delivered, Cancelled
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string ContactPhone { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }  // Pending, Completed, Failed

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
} 