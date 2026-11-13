using System.Reflection;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration
{
    public static class MappingHelpers
    {
        private static readonly Type[] _allowedTypes = new[]
        {
            typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid),
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(bool), typeof(Enum)
        };

        public static IReadOnlyDictionary<string, object> ToPropertyDictionary(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            var props = entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            foreach (var p in props)
            {
                var getMethod = p.GetMethod;
                if (getMethod == null) continue;
                var val = p.GetValue(entity);
                if (val == null) continue;
                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                if (!IsAllowedType(t)) continue;

                object? mapped = val;
                if (t == typeof(Guid)) mapped = val.ToString();
                if (t == typeof(ushort)) mapped = Convert.ToInt64(val);
                if (t.IsEnum) mapped = val.ToString();

                if (mapped != null)
                {
                    dict[p.Name] = mapped;
                }
            }

            return dict;
        }

        public static string NormalizeId(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (id is Guid g) return g.ToString();
            return id?.ToString() ?? string.Empty;
        }

        private static bool IsAllowedType(Type t)
        {
            if (t.IsEnum) return true;
            return _allowedTypes.Any(a => a == t || (a == typeof(Enum) && t.IsEnum));
        }
    }
}
