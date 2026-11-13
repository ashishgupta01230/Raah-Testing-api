using HautSphere.BuildingBlocks.Configuration.Filters;

namespace HautSphere.BuildingBlocks.Configuration.Extensions
{
    public static class FilterExtension
    {
        public static IServiceCollection AddMvcWithFilters(this IServiceCollection services)
        {
           services.AddScoped<TransactionFilter>();
            services.AddScoped<ExceptionFilter>();
            var mvcBuilder = services.AddControllers((opt) =>
            {
                opt.Filters.Add<TransactionFilter>();
                opt.Filters.Add(new ResponseFilterAttribute());
                opt.Filters.Add<ExceptionFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(
                    allowIntegerValues: false));
            }); ;
            return services;
        }
    }
}
