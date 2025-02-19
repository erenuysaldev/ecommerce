namespace ECommerceProject.Core.DTOs
{
    public class OrderSearchDto
    {
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SortBy { get; set; }  // date_desc, date_asc, amount_desc, amount_asc
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
} 