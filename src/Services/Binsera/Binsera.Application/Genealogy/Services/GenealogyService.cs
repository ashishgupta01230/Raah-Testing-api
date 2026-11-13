using Binsera.Application.Genealogy.Models;
using Binsera.Application.Members;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Neo4J.Configuration;
using Neo4j.Driver;

namespace Binsera.Application.Genealogy.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class GenealogyService : IGenealogyService
    {
        private readonly ClientAwareSessionProvider _clientAwareSessionProvider;
        public GenealogyService(ISessionProvider sessionProvider,
            ClientAwareSessionProvider clientAwareSessionProvider)
        {
            _clientAwareSessionProvider = clientAwareSessionProvider;
        }

        public async Task<Guid?> GetCompanyRootIdAsync()
        {
            // Every real member's SPONSORED chain terminates at a RegNo 1/2 bootstrap
            // placeholder (they have no parent of their own), so there is no real
            // member with zero SPONSORED parent. The real, visible root is instead
            // the topmost real member sponsored directly by one of those placeholders.
            var query = @"
                MATCH (bootstrap:Member {ClientId: $clientId})-[:SPONSORED]->(root:Member {ClientId: $clientId})
                WHERE bootstrap.RegNo IN [1, 2] AND NOT root.RegNo IN [1, 2]
                RETURN root.Id AS Id
                ORDER BY root.RegNo ASC
                LIMIT 1
            ";

            var records = await _clientAwareSessionProvider.ExecuteWriteAsync(query, new Dictionary<string, object>());
            var idValue = records.FirstOrDefault()?["Id"].As<string>();

            return Guid.TryParse(idValue, out var id) ? id : null;
        }

        public async Task<GenealogyNodeResponse> GetNodeAsync(Guid memberId, Guid currentMemberId, bool isAdmin)
        {
            // Admin can look up any node; a member can only look up a node that is
            // themselves or somewhere in their own downline (SPONSORED* below them).
            // RegNo 1 and 2 are pre-seeded bootstrap placeholders, never real members
            // -- they must never be surfaced to admin or member.
            var query = isAdmin ? @"
    MATCH (m:Member {Id: $memberId, ClientId: $clientId})
    WHERE NOT m.RegNo IN [1, 2]
    OPTIONAL MATCH (parent:Member {ClientId: $clientId})-[:SPONSORED]->(m)
    RETURN
        m,
        parent.Id AS ParentId,
        EXISTS {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS HasChildren,
        COUNT {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS ChildrenCount" : @"
    MATCH (me:Member {Id: $currentMemberId, ClientId: $clientId})
    MATCH (me)-[:SPONSORED*0..]->(m:Member {Id: $memberId, ClientId: $clientId})
    WHERE NOT m.RegNo IN [1, 2]
    OPTIONAL MATCH (parent:Member {ClientId: $clientId})-[:SPONSORED]->(m)
    RETURN
        m,
        parent.Id AS ParentId,
        EXISTS {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS HasChildren,
        COUNT {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS ChildrenCount";

            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["currentMemberId"] = currentMemberId.ToString()
            };

            var records = await _clientAwareSessionProvider.ExecuteWriteAsync(query, parameters);
            var results = records.ToList();
            if (results.Count == 0)
                throw new KeyNotFoundException($"Member {memberId} not found.");

            return MapNode(results[0]);
        }

        public async Task<List<GenealogyNodeResponse>> ExpandNodeAsync(Guid memberId, Guid currentMemberId, bool isAdmin)
        {
            var query = isAdmin ? @"
    MATCH (parent:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(m:Member {ClientId: $clientId})
    WHERE NOT m.RegNo IN [1, 2]

    RETURN
        m,
        parent.Id AS ParentId,
        EXISTS {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS HasChildren,
        COUNT {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS ChildrenCount

    ORDER BY m.FirstName
" : @"
    MATCH (me:Member {Id: $currentMemberId, ClientId: $clientId})
    MATCH (me)-[:SPONSORED*0..]->(parent:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(m:Member {ClientId: $clientId})
    WHERE NOT m.RegNo IN [1, 2]

    RETURN
        m,
        parent.Id AS ParentId,
        EXISTS {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS HasChildren,
        COUNT {
            (m)-[:SPONSORED]->(:Member {ClientId: $clientId})
        } AS ChildrenCount

    ORDER BY m.FirstName
";
            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["currentMemberId"] = currentMemberId.ToString()
            };

            var records = await _clientAwareSessionProvider.ExecuteWriteAsync(query, parameters);
            return records.Select(MapNode).ToList();
        }

        public async Task<GenealogySearchResponse> SearchMembersAsync(Guid currentMemberId, bool isAdmin, string keyword, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;

            string countQuery;
            string dataQuery;

            var parameters = new Dictionary<string, object>
            {
                ["keyword"] = keyword ?? string.Empty,
                ["skip"] = skip,
                ["limit"] = pageSize
            };

            if (isAdmin)
            {
                parameters["memberId"] = currentMemberId.ToString();

                countQuery = @"
            MATCH (m:Member {ClientId:$clientId})
            WHERE
                NOT m.RegNo IN [1, 2]
                AND (
                       toLower(m.FirstName) CONTAINS toLower($keyword)
                    OR toLower(m.LastName) CONTAINS toLower($keyword)
                    OR toString(m.RegNo) CONTAINS $keyword
                    OR m.RegistrationNumber CONTAINS $keyword
                )
            RETURN COUNT(m) AS TotalCount
        ";

                dataQuery = @"
            MATCH (m:Member {ClientId:$clientId})

            WHERE
                NOT m.RegNo IN [1, 2]
                AND (
                       toLower(m.FirstName) CONTAINS toLower($keyword)
                    OR toLower(m.LastName) CONTAINS toLower($keyword)
                    OR toString(m.RegNo) CONTAINS $keyword
                    OR m.RegistrationNumber CONTAINS $keyword
                )

            OPTIONAL MATCH (m)-[:SPONSORED]->(c:Member {ClientId:$clientId})

            RETURN
                m,
                null AS ParentId,
                COUNT(c) > 0 AS HasChildren,
                COUNT(c) AS ChildrenCount

            ORDER BY m.RegNo

            SKIP $skip

            LIMIT $limit
        ";
            }
            else
            {
                parameters["memberId"] = currentMemberId.ToString();

                countQuery = @"
            MATCH (me:Member {Id:$memberId, ClientId:$clientId})

            MATCH path=(me)-[:SPONSORED*0..]->(m:Member)

            WHERE
                NOT m.RegNo IN [1, 2]
                AND (
                       toLower(m.FirstName) CONTAINS toLower($keyword)
                    OR toLower(m.LastName) CONTAINS toLower($keyword)
                    OR toString(m.RegNo) CONTAINS $keyword
                    OR m.RegistrationNumber CONTAINS $keyword
                )

            RETURN COUNT(DISTINCT m) AS TotalCount
        ";

                dataQuery = @"
            MATCH (me:Member {Id:$memberId, ClientId:$clientId})

            MATCH path=(me)-[:SPONSORED*0..]->(m:Member)

            WHERE
                NOT m.RegNo IN [1, 2]
                AND (
                       toLower(m.FirstName) CONTAINS toLower($keyword)
                    OR toLower(m.LastName) CONTAINS toLower($keyword)
                    OR toString(m.RegNo) CONTAINS $keyword
                    OR m.RegistrationNumber CONTAINS $keyword
                )

            WITH DISTINCT m

            OPTIONAL MATCH (m)-[:SPONSORED]->(c:Member {ClientId:$clientId})

            RETURN
                m,
                null AS ParentId,
                COUNT(c) > 0 AS HasChildren,
                COUNT(c) AS ChildrenCount

            ORDER BY m.RegNo

            SKIP $skip

            LIMIT $limit
        ";
            }

            var countResult = await _clientAwareSessionProvider.ExecuteWriteAsync(countQuery, parameters);

            var totalCount = countResult.FirstOrDefault()?["TotalCount"].As<int>() ?? 0;

            var dataResult = await _clientAwareSessionProvider.ExecuteWriteAsync(dataQuery, parameters);

            return new GenealogySearchResponse
            {
                Items = dataResult.Select(MapNode).ToList(),
                TotalCount = totalCount
            };
        }

        public async Task<GenealogyPathResponse> GetMemberPathAsync(Guid memberId, Guid currentMemberId, bool isAdmin)
        {
            // Admin gets the path from the company root; a member's path is rooted
            // at themselves, so they can never see anything above their own node.
            var query = isAdmin ? @"
                MATCH path = (root:Member {ClientId: $clientId})-[:SPONSORED*0..]->(n:Member {Id: $memberId, ClientId: $clientId})
                WHERE NOT EXISTS { (root)<-[:SPONSORED]-(:Member {ClientId: $clientId}) }
                RETURN [node IN nodes(path) WHERE NOT node.RegNo IN [1, 2] | node.Id] AS Path
                LIMIT 1
            " : @"
                MATCH path = (root:Member {Id: $currentMemberId, ClientId: $clientId})-[:SPONSORED*0..]->(n:Member {Id: $memberId, ClientId: $clientId})
                RETURN [node IN nodes(path) WHERE NOT node.RegNo IN [1, 2] | node.Id] AS Path
                LIMIT 1
            ";
            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["currentMemberId"] = currentMemberId.ToString()
            };

            var records = await _clientAwareSessionProvider.ExecuteWriteAsync(query, parameters);
            var result = records.FirstOrDefault();

            return new GenealogyPathResponse
            {
                Path = result?["Path"].As<List<object>>()?.Select(x => x.ToString()!).ToList() ?? []
            };
        }

        public async Task<GenealogyChildrenResponse> GetChildrenPagedAsync(Guid memberId, Guid currentMemberId, bool isAdmin, int page = 1, int pageSize = 50)
        {
            var skip = (page - 1) * pageSize;

            var countQuery = isAdmin ? @"
                MATCH (n:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(child:Member {ClientId: $clientId})
                WHERE NOT child.RegNo IN [1, 2]
                RETURN count(child) AS TotalCount
            " : @"
                MATCH (me:Member {Id: $currentMemberId, ClientId: $clientId})
                MATCH (me)-[:SPONSORED*0..]->(n:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(child:Member {ClientId: $clientId})
                WHERE NOT child.RegNo IN [1, 2]
                RETURN count(child) AS TotalCount
            ";

            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["currentMemberId"] = currentMemberId.ToString()
            };

            var countResult = await _clientAwareSessionProvider.ExecuteWriteAsync(countQuery, parameters);
            var countRecord = countResult.FirstOrDefault();
            var totalCount = countRecord?["TotalCount"].As<int>() ?? 0;

            var dataQuery = isAdmin ? @"
                MATCH (parent:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(m:Member {ClientId: $clientId})
                WHERE NOT m.RegNo IN [1, 2]
                RETURN
                    m,
                    parent.Id AS ParentId,
                    EXISTS { (m)-[:SPONSORED]->(:Member {ClientId: $clientId}) } AS HasChildren,
                    COUNT { (m)-[:SPONSORED]->(:Member {ClientId: $clientId}) } AS ChildrenCount
                ORDER BY m.FirstName
                SKIP $skip
                LIMIT $limit
            " : @"
                MATCH (me:Member {Id: $currentMemberId, ClientId: $clientId})
                MATCH (me)-[:SPONSORED*0..]->(parent:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(m:Member {ClientId: $clientId})
                WHERE NOT m.RegNo IN [1, 2]
                RETURN
                    m,
                    parent.Id AS ParentId,
                    EXISTS { (m)-[:SPONSORED]->(:Member {ClientId: $clientId}) } AS HasChildren,
                    COUNT { (m)-[:SPONSORED]->(:Member {ClientId: $clientId}) } AS ChildrenCount
                ORDER BY m.FirstName
                SKIP $skip
                LIMIT $limit
            ";
            var dataParameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["currentMemberId"] = currentMemberId.ToString(),
                ["skip"] = skip,
                ["limit"] = pageSize
            };
            var dataResult = await _clientAwareSessionProvider.ExecuteWriteAsync(dataQuery, dataParameters);

            return new GenealogyChildrenResponse
            {
                Items = dataResult.Select(MapNode).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        private static GenealogyNodeResponse MapNode(IRecord record)
        {
            var node = record["m"].As<INode>();
            return new GenealogyNodeResponse
            {
                Id = node.Properties["Id"].As<string>(),
                RegNo = node.Properties["RegNo"].As<long>().ToString(),
                RegistrationNumber = node.Properties["RegistrationNumber"].As<string>(),
                JoiningDate = node.Properties["JoiningDate"].As<string>()?[..10],
                FullName = $"{node.Properties["FirstName"].As<string>()} {node.Properties["LastName"].As<string>()}",
                ParentId = record["ParentId"].As<object>()?.ToString(),
                HasChildren = record["HasChildren"].As<bool>(),
                ChildrenCount = record["ChildrenCount"].As<int>()
            };
        }
    }
}
