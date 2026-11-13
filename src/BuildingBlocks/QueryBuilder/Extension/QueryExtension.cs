using HautSphere.BuildingBlocks.QueryBuilder.Models;
using System.Linq;

namespace HautSphere.BuildingBlocks.QueryBuilder.Extension
{
    public static class QueryExtension
    {
        public static string SelectQuery<Table, Selector>()
        {
            var selectColumns = typeof(Table).GetProperties().Select(s => s.Name.ToLower());
            var columnNames = typeof(Selector).GetProperties().Where(f => selectColumns.Contains(f.Name.ToLower()))
                .Select(p => p.Name.ToLower());

            return $"select {string.Join(',', columnNames)} from {typeof(Table).Name}";
        }
        public static string CountQuery<Table>()
        {
            return $"select count(1) from {typeof(Table).Name}";
        }
        public static string OrderByQuery<Table>(params OrderByInfo[] columnName)
        {
            if (columnName == null || columnName.Any(x => x == null)) return string.Empty;
            var columnNames = typeof(Table).GetProperties().Select(p => p.Name.ToLower());
            var verifiedColumns = columnName.Where(s => columnNames.Contains(s.ColumnName.ToLower()))
                .Select(o => $"{o.ColumnName.ToLower()}{o.CastTo?.ToLower()} {o.SortBy.ToString().ToLower()} {NullableSorting(o.SortingNullablePosition)}");
            if (!verifiedColumns.Any()) return string.Empty;
            return $"order by {string.Join(',', verifiedColumns)}";
        }
        private static string NullableSorting(SortingNullablePosition SortingNullablePosition)
        {
            return SortingNullablePosition != SortingNullablePosition.None ? $"nulls {SortingNullablePosition.ToString().ToLower()}" : string.Empty;
        }
    }
}
