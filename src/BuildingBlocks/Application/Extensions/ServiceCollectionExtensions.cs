using HautSphere.BuildingBlocks.Application.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HautSphere.BuildingBlocks.Application.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection to support auto-registration of services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Scans assemblies and automatically registers services marked with the AutoRegister attribute.
        /// Only scans assemblies matching 'MC.*' or 'MasterCRM.*' patterns.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when registration validation fails.</exception>
        public static IServiceCollection AddAutoRegisteredServices(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic &&
                            a.FullName?.StartsWith("Binsera.", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var assembly in assemblies)
            {
                RegisterServicesFromAssembly(services, assembly);
            }

            return services;
        }

        private static void RegisterServicesFromAssembly(IServiceCollection services, Assembly assembly)
        {
            var typesWithAttribute = assembly.GetTypes()
                .Where(t => t.IsClass &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<AutoRegisterAttribute>() != null)
                .ToList();

            foreach (var implementationType in typesWithAttribute)
            {
                var attribute = implementationType.GetCustomAttribute<AutoRegisterAttribute>()!;

                ValidateRegistration(implementationType, attribute);

                if (attribute.IncludeInterfaces)
                {
                    RegisterAllInterfaces(services, implementationType, attribute.ServiceLifetime);
                }
                else if (attribute.ServiceType != null)
                {
                    RegisterAgainstInterface(services, implementationType, attribute.ServiceType, attribute.ServiceLifetime);
                }
                else
                {
                    RegisterAsSelf(services, implementationType, attribute.ServiceLifetime);
                }
            }
        }

        private static void ValidateRegistration(Type implementationType, AutoRegisterAttribute attribute)
        {
            // Validate: must be a concrete class
            if (!implementationType.IsClass || implementationType.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"Type '{implementationType.FullName}' marked with AutoRegister must be a concrete class.");
            }

            // Validate: must have a public constructor
            var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type '{implementationType.FullName}' marked with AutoRegister must have at least one public constructor.");
            }

            // Validate: if ServiceType is specified, implementation must implement it
            if (attribute.ServiceType != null)
            {
                if (!attribute.ServiceType.IsAssignableFrom(implementationType))
                {
                    throw new InvalidOperationException(
                        $"Type '{implementationType.FullName}' does not implement specified service type '{attribute.ServiceType.FullName}'.");
                }
            }
        }

        private static void RegisterAllInterfaces(
            IServiceCollection services,
            Type implementationType,
            ServiceLifetime lifetime)
        {
            var interfaces = implementationType.GetInterfaces()
                .Where(i => i != typeof(IDisposable) && i != typeof(IAsyncDisposable))
                .ToList();

            if (interfaces.Count == 0)
            {
                // No interfaces - register as self
                RegisterAsSelf(services, implementationType, lifetime);
                return;
            }

            foreach (var interfaceType in interfaces)
            {
                RegisterService(services, interfaceType, implementationType, lifetime);
            }
        }

        private static void RegisterAgainstInterface(
            IServiceCollection services,
            Type implementationType,
            Type serviceType,
            ServiceLifetime lifetime)
        {
            RegisterService(services, serviceType, implementationType, lifetime);
        }

        private static void RegisterAsSelf(
            IServiceCollection services,
            Type implementationType,
            ServiceLifetime lifetime)
        {
            RegisterService(services, implementationType, implementationType, lifetime);
        }

        private static void RegisterService(
            IServiceCollection services,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.AddTransient(serviceType, implementationType);
                    break;

                case ServiceLifetime.Scoped:
                    services.AddScoped(serviceType, implementationType);
                    break;

                case ServiceLifetime.Singleton:
                    services.AddSingleton(serviceType, implementationType);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(lifetime),
                        lifetime,
                        $"Unsupported service lifetime: {lifetime}");
            }
        }

    }
}
