using HautSphere.BuildingBlocks.QueryBuilder.Providers;
using HautSphere.BuildingBlocks.QueryBuilder.Query;
using Microsoft.Extensions.DependencyInjection;

namespace HautSphere.BuildingBlocks.QueryBuilder.Extension
{
    public static class ServiceExtension
    {
        public static void AddDapperForPostgress(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<IDapperQuery, PostgressDapper>(x => new PostgressDapper(connectionString));
        }

        public static void AddDapperForMsSql(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<IDapperQuery, MsSqlDapper>(x => new MsSqlDapper(connectionString));
        }
    }
}
