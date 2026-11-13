namespace Binsera.Application.Members
{
    using Binsera.Core;
    using HautSphere.BuildingBlocks.Neo4J.Configuration;
    using Neo4j.Driver;

    public sealed class ClientAwareSessionProvider
    {
        private readonly ISessionProvider _sessionProvider;

        public ClientAwareSessionProvider(
            ISessionProvider sessionProvider)
        {
            _sessionProvider = sessionProvider;
        }

        public async Task<IReadOnlyList<IRecord>> ExecuteWriteAsync(
            string cypher,
            Dictionary<string, object>? parameters = null)
        {
            parameters ??= new();

            parameters["clientId"] = CompanySetting.Default.ClientId;

            return await _sessionProvider.ExecuteWriteAsync(
                cypher,
                parameters);
        }
    }
}
