using Asp.Versioning;

namespace HautSphere.BuildingBlocks.Configuration.Extensions
{
    public static class ApiVersioningExtension
    {
        public static void AddApiVersion(this IServiceCollection services)
        {
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
            });
        }
    }
}
