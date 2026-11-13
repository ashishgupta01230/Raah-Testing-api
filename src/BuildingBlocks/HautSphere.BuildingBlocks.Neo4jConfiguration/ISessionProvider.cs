using Neo4j.Driver;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration
{
    public interface ISessionProvider : IAsyncDisposable
    {
        IAsyncSession OpenSession();
        Task<IReadOnlyList<IRecord>> ExecuteWriteAsync(string cypher, IReadOnlyDictionary<string, object> parameters);
    }
}
