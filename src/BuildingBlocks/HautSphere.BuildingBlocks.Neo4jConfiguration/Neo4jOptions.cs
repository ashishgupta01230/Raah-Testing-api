namespace HautSphere.BuildingBlocks.Neo4J.Configuration;

public sealed class Neo4jOptions
{
    public string Uri { get; set; } = "bolt://localhost:7687";
    public string Username { get; set; } = "neo4j";
    public string Password { get; set; } = string.Empty;
    public string? Database { get; set; }
    public string IdentityPropertyName { get; set; } = "id";
}
