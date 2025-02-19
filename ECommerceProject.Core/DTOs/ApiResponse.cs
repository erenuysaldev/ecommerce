namespace ECommerceProject.Core.DTOs
{
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public string Error { get; set; }
        public List<string> ValidationErrors { get; set; }
        public object Meta { get; set; }
        public bool IsSuccessful => string.IsNullOrEmpty(Error) && (ValidationErrors == null || !ValidationErrors.Any());
    }
} 