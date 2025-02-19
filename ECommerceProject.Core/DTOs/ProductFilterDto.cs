namespace ECommerceProject.Core.DTOs
{
    public class ProductFilterDto
    {
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? CategoryId { get; set; }
        public string? SortBy { get; set; } // Name, Price, Stock
        public string? SortDirection { get; set; } // ASC, DESC
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
