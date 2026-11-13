namespace HautSphere.BuildingBlocks.Neo4J.Configuration.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Health check for Neo4j connectivity and query execution.
/// Implements ASP.NET Core IHealthCheck contract to validate Neo4j is responding.
/// </summary>
public class Neo4jHealthCheck : IHealthCheck
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the Neo4jHealthCheck class.
    /// </summary>
    /// <param name="driver">Neo4j driver instance (injected via DI)</param>
    /// <param name="logger">Logger instance (injected via DI)</param>
    /// <exception cref="ArgumentNullException">Thrown if driver or logger is null</exception>
    public Neo4jHealthCheck(IDriver driver, ILogger<Neo4jHealthCheck> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks Neo4j health by executing a simple read query.
    /// </summary>
    /// <param name="context">Health check context</param>
    /// <param name="cancellationToken">Cancellation token to support cancellation</param>
    /// <returns>HealthCheckResult indicating healthy or unhealthy status</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Execute lightweight read query to validate Neo4j is responding
            // Use session pattern consistent with the rest of the codebase
            await using var session = _driver.AsyncSession();
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1", new Dictionary<string, object>());
                return await cursor.ToListAsync();
            });

            return HealthCheckResult.Healthy();
        }
        catch (ClientException ex)
        {
            _logger.LogError(ex, "Neo4j health check failed: ClientException");
            return HealthCheckResult.Unhealthy("Neo4j client error", ex);
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Neo4j health check failed: ServiceUnavailableException");
            return HealthCheckResult.Unhealthy("Neo4j service unavailable", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Neo4j health check failed: TimeoutException");
            return HealthCheckResult.Unhealthy("Neo4j timeout", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Neo4j health check failed: OperationCanceledException");
            return HealthCheckResult.Unhealthy("Neo4j operation cancelled", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo4j health check failed: {ExceptionType}", ex.GetType().Name);
            return HealthCheckResult.Unhealthy("Neo4j health check failed", ex);
        }
    }
}

