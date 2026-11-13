using System.Diagnostics.Metrics;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration.Observability;

internal static class Neo4jMetrics
{
    private static readonly Meter _meter = new("MasterCRM.BuildingBlocks.Neo4J", "1.0.0");
    internal static readonly Counter<long> SyncRequests = _meter.CreateCounter<long>("neo4j_sync_requests_total");
    internal static readonly Counter<long> SyncErrors = _meter.CreateCounter<long>("neo4j_sync_errors_total");
    internal static readonly Histogram<double> SyncDuration = _meter.CreateHistogram<double>("neo4j_sync_duration_seconds");
}
