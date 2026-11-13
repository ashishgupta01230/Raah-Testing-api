using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HautSphere.BuildingBlocks.Logger
{
    public static class LoggerConfigurationExtension
    {
        public static void AddLogger(this IHostBuilder host)
        {
            var configurationSection = new ConfigurationBuilder()
              .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
              .AddJsonFile("serilog.settings.json", optional: false, reloadOnChange: true)
              .Build();
            host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(configurationSection));
        }
    }
}
