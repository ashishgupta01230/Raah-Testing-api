namespace HautSphere.BuildingBlocks.Configuration.Extensions
{
    public static class CorsPolicyExtension
    {
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                var policyName = configuration["CorsPolicy"];
                options.AddPolicy(!string.IsNullOrEmpty(policyName) ? policyName : "CorsPolicy",
                    builder => builder
                        .SetIsOriginAllowed((host) => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Content-Disposition")
                );
            });

            return services;
        }

        public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app, IConfiguration configuration)
        {
            var policyName = configuration["CorsPolicy"];
            app.UseCors(!string.IsNullOrEmpty(policyName) ? policyName : "CorsPolicy");
            return app;
        }
    }
}
