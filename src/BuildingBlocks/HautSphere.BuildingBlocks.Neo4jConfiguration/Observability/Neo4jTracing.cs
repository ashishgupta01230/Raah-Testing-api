using System.Diagnostics;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration.Observability;

internal static class Neo4jTracing
{
    internal static readonly ActivitySource ActivitySource = new("MasterCRM.BuildingBlocks.Neo4J", "1.0.0");
}
