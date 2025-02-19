namespace ECommerceProject.Core.DTOs
{
    public class UpdateStockDto
    {
        public int ProductId { get; set; }
        public int NewStock { get; set; }
    }

    public class BulkUpdateStockDto
    {
        public List<UpdateStockDto> Products { get; set; }
    }
} 