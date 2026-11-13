using Microsoft.Extensions.DependencyInjection;

namespace HautSphere.BuildingBlocks.Application.Attributes
{
    /// <summary>
    /// Marks a class for automatic registration in the dependency injection container.
    /// The class will be discovered and registered during application startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AutoRegisterAttribute : Attribute
    {
        /// <summary>
        /// Gets the service lifetime for dependency injection registration.
        /// </summary>
        public ServiceLifetime ServiceLifetime { get; }

        /// <summary>
        /// Gets the service type (typically an interface) to register against.
        /// If null, the class will be registered as itself.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Gets whether to register the class against all implemented interfaces.
        /// If true, ignores ServiceType and registers all interfaces.
        /// </summary>
        public bool IncludeInterfaces { get; }

        /// <summary>
        /// Initializes a new instance of the AutoRegisterAttribute with the specified service lifetime.
        /// The class will be registered as itself (no interface).
        /// </summary>
        /// <param name="serviceLifetime">The service lifetime.</param>
        public AutoRegisterAttribute(ServiceLifetime serviceLifetime)
        {
            ServiceLifetime = serviceLifetime;
            ServiceType = null;
            IncludeInterfaces = false;
        }

        /// <summary>
        /// Initializes a new instance of the AutoRegisterAttribute with a specific service type.
        /// </summary>
        /// <param name="serviceLifetime">The service lifetime.</param>
        /// <param name="serviceType">The service type (interface) to register against.</param>
        /// <param name="includeInterfaces">If true, registers all implemented interfaces instead of just serviceType.</param>
        public AutoRegisterAttribute(
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
            Type serviceType = default,
            bool includeInterfaces = false)
        {
            ServiceLifetime = serviceLifetime;
            ServiceType = serviceType;
            IncludeInterfaces = includeInterfaces;
        }

    }
}
