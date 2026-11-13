namespace HautSphere.BuildingBlocks.QueryBuilder.Models
{
    public class OrderByInfo
    {
        public SortingDirection SortBy { get; }
        public string ColumnName { get; }
        public string CastTo { get; }
        public SortingNullablePosition SortingNullablePosition { get; set; }
        public OrderByInfo(string columnName, int direction, string castTo = "")
        {
            ColumnName = columnName;
            SortBy = (SortingDirection)direction;
            CastTo = castTo;
            SortingNullablePosition = SortingNullablePosition.None;
        }
    }
}
