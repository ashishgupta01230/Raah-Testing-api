namespace Binsera.Core.Orders.Models
{
    public sealed class AdminOrderListResponseDto
    {
        public List<AdminOrderListDto> Orders { get; set; } = new List<AdminOrderListDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}
