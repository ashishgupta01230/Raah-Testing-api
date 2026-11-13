namespace HautSphere.BuildingBlocks.Domain.Models
{
    public abstract class PaginationModel
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public SortingOrder SortingOrder { get; set; } = SortingOrder.Asc;
        public string ColName { get; set; }
    }
    public enum SortingOrder
    {
        Desc = 1,
        Asc = 0
    }
}
