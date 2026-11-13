using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration
{
    public class GraphEvent
    {
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public object? Entity { get; set; }
        public List<RelationDescriptor>? Relations { get; set; }
    }

    public class RelationDescriptor
    {
        public string? RelationshipType { get; set; }
        public string? TargetEntityType { get; set; }
        public string? TargetEntityId { get; set; }
        public RelationDirection Direction { get; set; } = RelationDirection.Outgoing;
        public object? Properties { get; set; }
    }

    public enum RelationDirection
    {
        Outgoing,
        Incoming
    }

    public class SynchronizationService
    {
        private readonly ISessionProvider _provider;
        private readonly ILogger<SynchronizationService> _logger;
        private readonly string _identityPropertyName;

        public SynchronizationService(ISessionProvider provider, ILogger<SynchronizationService> logger, IOptions<Neo4jOptions> options)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identityPropertyName = options?.Value?.IdentityPropertyName ?? "id";
            IdentifierValidator.ThrowIfInvalid(_identityPropertyName, nameof(options.Value.IdentityPropertyName));
        }

        public async Task<IReadOnlyList<Neo4j.Driver.IRecord>> Sync(GraphEvent ev)
        {
            if (ev == null) throw new ArgumentNullException(nameof(ev));
            if (string.IsNullOrWhiteSpace(ev.EntityType)) throw new ArgumentException("EntityType is required", nameof(ev.EntityType));
            if (string.IsNullOrWhiteSpace(ev.EntityId)) throw new ArgumentException("EntityId is required", nameof(ev.EntityId));

            IdentifierValidator.ThrowIfInvalid(ev.EntityType, nameof(ev.EntityType));

            // Normalize EntityId (Guid -> string)
            var normalizedEntityId = MappingHelpers.NormalizeId(ev.EntityId!);

            var props = ev.Entity != null ? MappingHelpers.ToPropertyDictionary(ev.Entity) : new Dictionary<string, object>();

            // Build single Cypher script combining node MERGE + SET and relations
            var cypherBuilder = new System.Text.StringBuilder();
            var parameters = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [_identityPropertyName] = normalizedEntityId
            };

            // If payload entity is null -> this is a delete request: emit MATCH .. DETACH DELETE
            if (ev.Entity == null)
            {
                cypherBuilder.Append($"MATCH (n:{ev.EntityType} {{{_identityPropertyName}: ${_identityPropertyName}}})\nDETACH DELETE n\n");
            }
            else
            {
                // Node MERGE and SET
                var setClauses = props.Any() ? string.Join(", ", props.Keys.Select(k => $"n.{k} = ${k}")) : string.Empty;
                if (setClauses.Length > 0)
                {
                    cypherBuilder.Append($"MERGE (n:{ev.EntityType} {{{_identityPropertyName}: ${_identityPropertyName}}})\nSET {setClauses}\n");
                }
                else
                {
                    cypherBuilder.Append($"MERGE (n:{ev.EntityType} {{{_identityPropertyName}: ${_identityPropertyName}}})\n");
                }
            }

            // Add node properties to parameters
            foreach (var kv in props)
            {
                parameters[kv.Key] = kv.Value;
            }

            // Relations: generate MATCH/MERGE blocks using indexed parameter names to avoid collisions
            if (ev.Relations != null && ev.Relations.Any())
            {
                int relIndex = 0;
                foreach (var r in ev.Relations)
                {
                    if (string.IsNullOrWhiteSpace(r.TargetEntityType) || string.IsNullOrWhiteSpace(r.TargetEntityId) || string.IsNullOrWhiteSpace(r.RelationshipType))
                        throw new ArgumentException("Relation descriptor missing required fields");

                    IdentifierValidator.ThrowIfInvalid(r.TargetEntityType, nameof(r.TargetEntityType));
                    IdentifierValidator.ThrowIfInvalid(r.RelationshipType, nameof(r.RelationshipType));

                    var tgtParam = $"tgtId{relIndex}";
                    parameters[tgtParam] = MappingHelpers.NormalizeId(r.TargetEntityId!);

                    var relProps = r.Properties != null ? MappingHelpers.ToPropertyDictionary(r.Properties) : new Dictionary<string, object>();
                    foreach (var kv in relProps)
                    {
                        var paramName = $"rel_{relIndex}_{kv.Key}";
                        parameters[paramName] = kv.Value;
                    }

                    // Build rel SET clause with unique param names, ensure variable r{index} is referenced
                    var relSetClause = relProps.Any()
                        ? " SET " + string.Join(", ", relProps.Keys.Select(k => $"r{relIndex}.{k} = $rel_{relIndex}_{k}"))
                        : string.Empty;

                    if (r.Direction == RelationDirection.Outgoing)
                    {
                        cypherBuilder.Append($"MERGE (target{relIndex}:{r.TargetEntityType} {{{_identityPropertyName}: ${tgtParam}}})\nMERGE (n)-[r{relIndex}:{r.RelationshipType}]->(target{relIndex}){relSetClause}\n");
                    }
                    else
                    {
                        cypherBuilder.Append($"MERGE (target{relIndex}:{r.TargetEntityType} {{{_identityPropertyName}: ${tgtParam}}})\nMERGE (target{relIndex})-[r{relIndex}:{r.RelationshipType}]->(n){relSetClause}\n");
                    }

                    relIndex++;
                }
            }

            var cypher = cypherBuilder.ToString();
            _logger.LogInformation("Executing Neo4j sync for {EntityType}/{EntityId}", ev.EntityType, normalizedEntityId);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var span = Observability.Neo4jTracing.ActivitySource.StartActivity("neo4j.sync");
            try
            {
                Observability.Neo4jMetrics.SyncRequests.Add(1);
                var result = await _provider.ExecuteWriteAsync(cypher, parameters);
                sw.Stop();
                Observability.Neo4jMetrics.SyncDuration.Record(sw.Elapsed.TotalSeconds);
                var safeResult = result ?? (IReadOnlyList<Neo4j.Driver.IRecord>)Array.Empty<Neo4j.Driver.IRecord>();
                _logger.LogInformation("Neo4j sync completed for {EntityType}/{EntityId}; records: {Count}", ev.EntityType, normalizedEntityId, safeResult.Count);
                return safeResult;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Observability.Neo4jMetrics.SyncErrors.Add(1);
                Observability.Neo4jMetrics.SyncDuration.Record(sw.Elapsed.TotalSeconds);
                _logger.LogError(ex, "Neo4j sync failed for {EntityType}/{EntityId}", ev.EntityType, normalizedEntityId);
                span?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
                throw;
            }
        }

        public Task<IReadOnlyList<Neo4j.Driver.IRecord>> UpdateProperty(string label, string id, string propertyName, object value)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("label required", nameof(label));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id required", nameof(id));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("propertyName required", nameof(propertyName));

            IdentifierValidator.ThrowIfInvalid(label, nameof(label));
            IdentifierValidator.ThrowIfInvalid(propertyName, nameof(propertyName));

            var normalizedId = MappingHelpers.NormalizeId(id);
            var cypher = $"MATCH (n:{label} {{{_identityPropertyName}: $id}}) SET n.{propertyName} = $value";
            var parameters = new Dictionary<string, object>
            {
                ["id"] = normalizedId,
                ["value"] = value!
            };

            _logger.LogInformation("Updating property {Property} on {Label}:{Id}", propertyName, label, normalizedId);
            var result = _provider.ExecuteWriteAsync(cypher, parameters);
            _logger.LogInformation("UpdateProperty enqueued for {Label}:{Id}", label, normalizedId);
            return result;
        }

        public Task<IReadOnlyList<Neo4j.Driver.IRecord>> DeleteByProperty(string label, string propertyName, object value)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("label required", nameof(label));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("propertyName required", nameof(propertyName));

            IdentifierValidator.ThrowIfInvalid(label, nameof(label));
            IdentifierValidator.ThrowIfInvalid(propertyName, nameof(propertyName));

            var cypher = $"MATCH (n:{label}) WHERE n.{propertyName} = $value DETACH DELETE n";
            var parameters = new Dictionary<string, object>
            {
                ["value"] = value!
            };

            using var span = Observability.Neo4jTracing.ActivitySource.StartActivity("neo4j.delete");
            _logger.LogInformation("Deleting nodes by property {Label}.{Property}", label, propertyName);
            try
            {
                var result = _provider.ExecuteWriteAsync(cypher, parameters);
                _logger.LogInformation("DeleteByProperty enqueued for {Label}.{Property}", label, propertyName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteByProperty failed for {Label}.{Property}", label, propertyName);
                span?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<(int matched, int deleted)> DeleteByPropertySafeAsync(string label, string propertyName, object value, bool dryRun = true)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("label required", nameof(label));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("propertyName required", nameof(propertyName));

            IdentifierValidator.ThrowIfInvalid(label, nameof(label));
            IdentifierValidator.ThrowIfInvalid(propertyName, nameof(propertyName));

            // 1) count matching nodes
            var countCypher = $"MATCH (n:{label}) WHERE n.{propertyName} = $value RETURN count(n) AS cnt";
            var parameters = new Dictionary<string, object>
            {
                ["value"] = value!
            };

            using var span = Observability.Neo4jTracing.ActivitySource.StartActivity("neo4j.delete.safe");
            try
            {
                var countResult = await _provider.ExecuteWriteAsync(countCypher, parameters).ConfigureAwait(false);
                var matched = 0;
                if (countResult != null && countResult.Count > 0)
                {
                    var rec = countResult[0];
                    if (rec.Values != null && rec.Values.ContainsKey("cnt"))
                    {
                        var cntObj = rec["cnt"];
                        if (cntObj is long l) matched = (int)l;
                        else if (cntObj is int i) matched = i;
                        else
                        {
                            try { matched = Convert.ToInt32(cntObj); } catch { matched = 0; }
                        }
                    }
                }

                if (matched == 0 || dryRun)
                {
                    return (matched, 0);
                }

                var deleteCypher = $"MATCH (n:{label}) WHERE n.{propertyName} = $value DETACH DELETE n RETURN count(n) AS deleted";
                var delResult = await _provider.ExecuteWriteAsync(deleteCypher, parameters).ConfigureAwait(false);
                var deleted = 0;
                if (delResult != null && delResult.Count > 0)
                {
                    var rec = delResult[0];
                    if (rec.Values != null && rec.Values.ContainsKey("deleted"))
                    {
                        var delObj = rec["deleted"];
                        if (delObj is long l) deleted = (int)l;
                        else if (delObj is int i) deleted = i;
                        else
                        {
                            try { deleted = Convert.ToInt32(delObj); } catch { deleted = 0; }
                        }
                    }
                }

                _logger.LogInformation("DeleteByPropertySafe executed for {Label}.{Property}: matched={Matched}, deleted={Deleted}", label, propertyName, matched, deleted);
                return (matched, deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteByPropertySafe failed for {Label}.{Property}", label, propertyName);
                span?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
                throw;
            }
        }
    }
}
