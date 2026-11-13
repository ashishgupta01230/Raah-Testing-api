using HautSphere.BuildingBlocks.Domain.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace HautSphere.BuildingBlocks.Persistence.Extensions
{
    public static class SortHelper
    {
        public static string OrderBy<TEntity>(string orderByProperty, SortingOrder? sortingOrder = SortingOrder.Asc, Expression<Func<TEntity, object>> sortExpression = null)
        {
            orderByProperty = orderByProperty?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(orderByProperty))
            {
                var propertyInfo = typeof(TEntity).GetProperty(orderByProperty, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new InvalidCastException(string.Format("Some exceptions", orderByProperty));
            }
            else if (sortExpression != null)
            {
                orderByProperty = sortExpression.ToStringForOrdering();
            }
            return orderByProperty + (!string.IsNullOrWhiteSpace(orderByProperty) ? " " + sortingOrder.ToString() : null);
        }
    }

    public static class ExpressionExtensions
    {
        public static string ToStringForOrdering<T, TKey>(this Expression<Func<T, TKey>> expression, bool isDesc = false)
        {
            var str = expression.Body.ToString();
            var param = expression.Parameters.First().Name;
            str = str.Replace("Convert(", "").Replace(", Object)", "").Replace(param + ".", "");
            return str + (isDesc ? " desc" : "");
        }
    }
}
