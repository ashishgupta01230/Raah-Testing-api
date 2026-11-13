using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddNeo4j(
        this IServiceCollection services,
        Action<Neo4jOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Register options for IOptions<T> consumers
        services.Configure(configure);

        // Validate options eagerly
        var tmp = new Neo4jOptions();
        configure(tmp);
        if (string.IsNullOrWhiteSpace(tmp.Uri)) throw new ArgumentException("Neo4j Uri is required", nameof(tmp.Uri));
        if (string.IsNullOrWhiteSpace(tmp.Username)) throw new ArgumentException("Neo4j Username is required", nameof(tmp.Username));
        IdentifierValidator.ThrowIfInvalid(tmp.IdentityPropertyName, nameof(tmp.IdentityPropertyName));

        // Register IDriver singleton using configured options
        services.AddSingleton<IDriver>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            if (!Uri.TryCreate(opts.Uri, UriKind.Absolute, out var url))
                throw new ArgumentException($"Invalid Neo4j Uri: {opts.Uri}");

            if (url.Scheme != "bolt" && url.Scheme != "neo4j" && url.Scheme != "bolt+s" && url.Scheme != "neo4j+s")
                throw new ArgumentException($"Neo4j Uri scheme must be bolt://, neo4j://, bolt+s:// or neo4j+s:// (got: {url.Scheme})");

            var auth = AuthTokens.Basic(opts.Username, opts.Password ?? string.Empty);
            return GraphDatabase.Driver(url, auth);
        });

        // Register ISessionProvider and SynchronizationService as scoped
        // ISessionProvider requires the configured database name (may be null to use server default)
        services.AddScoped<ISessionProvider>(sp =>
        {
            var driver = sp.GetRequiredService<IDriver>();
            var opts = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            return new SessionProvider(driver, opts.Database);
        });

        services.AddScoped<SynchronizationService>();

        return services;
    }
}
