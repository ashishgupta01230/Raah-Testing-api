using Neo4j.Driver;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration
{
    public class SessionProvider : ISessionProvider
    {
        private readonly IDriver _driver;
        private readonly string? _database;
        private bool _disposed;

        public SessionProvider(IDriver driver, string? database = null)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _database = database;
        }

        public IAsyncSession OpenSession()
        {
            return _driver.AsyncSession(o => { if (!string.IsNullOrEmpty(_database)) o.WithDatabase(_database); o.WithDefaultAccessMode(AccessMode.Write); });
        }

        public async Task<IReadOnlyList<IRecord>> ExecuteWriteAsync(string cypher, IReadOnlyDictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(cypher)) throw new ArgumentException("cypher is required", nameof(cypher));
            await using var session = OpenSession();
            return await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters ?? new Dictionary<string, object>());
                return await cursor.ToListAsync();
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    // Do NOT close the shared IDriver here. The driver is registered as a singleton
                    // and will be disposed by the DI container when the application/service provider
                    // is shutting down. Closing it here (from a scoped SessionProvider) would
                    // make the driver unusable for other scopes and cause ObjectDisposedException.
                    // If ownership semantics are required in the future, change the DI registration
                    // and implement explicit ownership.
                    await Task.CompletedTask;
                }
                catch
                {
                    // Ignore errors during disposal
                }
                _disposed = true;
            }
        }
    }
}
