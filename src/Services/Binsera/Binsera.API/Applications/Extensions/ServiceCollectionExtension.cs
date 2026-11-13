using Binsera.Application;
using Binsera.Persistence;
using Binsera.Persistence.Interceptors;
using HautSphere.BuildingBlocks.Application;
using HautSphere.BuildingBlocks.Application.EventDispatcher;
using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.QueryBuilder.Extension;
using Microsoft.EntityFrameworkCore;

namespace Binsera.API.Applications.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddBinseraDbContext(this IServiceCollection services,
            string dbConnection)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddDbContext<BinseraDbContext>(builder =>
              builder.UseSqlServer(dbConnection, sqlOptions =>
              {
                  sqlOptions.MigrationsHistoryTable("_efmigrationshistory");
              }));
            services.AddDapperForMsSql(dbConnection);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        public static IServiceCollection AddDomainEvents(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Scan all assemblies for types implementing IDomainEventHandler<>
            var handlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
                .Where(x => x.Interface.IsGenericType &&
                            x.Interface.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .ToList();

            foreach (var handler in handlerTypes)
            {
                services.AddScoped(handler.Interface, handler.Type);
            }
            var externalHandlerTypes = assemblies
                                    .SelectMany(a => a.GetTypes())
                                    .Where(t => !t.IsAbstract && !t.IsInterface)
                                    .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
                                    .Where(x => x.Interface.IsGenericType &&
                                                x.Interface.GetGenericTypeDefinition() == typeof(IExternalEventHandler<>))
                                    .ToList();

            foreach (var handler in externalHandlerTypes)
            {
                services.AddScoped(handler.Interface, handler.Type);
            }

            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }
    }
}
