using Binsera.Application.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using Neo4j.Driver;

namespace Binsera.Application.Members.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class GraphDatabaseMemberQueryService : IGraphDatabaseMemberQueryService
    {
        private readonly ClientAwareSessionProvider _sessionProvider;

        public GraphDatabaseMemberQueryService(ClientAwareSessionProvider sessionProvider)
        {
            _sessionProvider = sessionProvider;
        }

        public async Task<TreeResponseModel> GetMemberTreeAsync(int memberId, int depth)
        {
            var query = $@"
        MATCH (loginRoot:Member {{
            RegNo: $loginRootRegno, ClientId: $clientId
        }})
        MATCH (currentRoot:Member {{
            RegNo: $regno, ClientId: $clientId
        }})
        CALL {{
            WITH currentRoot
            MATCH path =
            (currentRoot)-[:PLACED*0..{depth}]->
            (node:Member {{ClientId: $clientId}})
            WHERE NOT node.RegNo IN [1, 2]
            RETURN
                node,
                path
        }}

        OPTIONAL MATCH
        (parent:Member {{ClientId: $clientId}})-[rel:PLACED]->(node)
        OPTIONAL MATCH
        (node)-[:PLACED {{
            side:'L'
        }}]->(leftChild:Member {{ClientId: $clientId}})
        OPTIONAL MATCH
        (node)-[:PLACED {{
            side:'R'
        }}]->(rightChild:Member {{ClientId: $clientId}})

        WITH
            loginRoot,
            currentRoot,
            node,
            parent,
            rel,
            path,
            leftChild,
            rightChild,
            COUNT {{
                (node)-[:PLACED]->(:Member {{ClientId: $clientId}})
            }}
            AS childrenCount
        RETURN
            node,
            parent.RegNo
            AS parentId,
            rel.side
            AS placement,
            length(path)
            AS level,
            leftChild IS NOT NULL
            AS leftChildExists,
            rightChild IS NOT NULL
            AS rightChildExists,
            childrenCount,
            childrenCount > 0
            AS hasChildren,
            loginRoot.RegNo
            AS loginRootId,
            currentRoot.RegNo
            AS currentRootId
        ";
            var parameters = new Dictionary<string, object>
            {
                ["regno"] = memberId,
                ["loginRootRegno"] = 1
            };

            var results = await _sessionProvider.ExecuteWriteAsync(query, parameters);
            var records = results.ToList();
            if (!records.Any()) return new TreeResponseModel();

            var members = new List<TreeMemberDto>();
            var added = new HashSet<string>();
            foreach (var record in records)
            {
                var node = record["node"].As<INode>();
                var member = MapTreeMember(record, node);
                if (added.Add(member.Id))
                    members.Add(member);
            }

            var virtualNodes = new List<TreeMemberDto>();
            foreach (var member in members)
            {
                if (!member.LeftChildExists)
                    virtualNodes.Add(CreateEmptyNode(member, "L"));
                if (!member.RightChildExists)
                    virtualNodes.Add(CreateEmptyNode(member, "R"));
            }
            members.AddRange(virtualNodes);

            var root = members.FirstOrDefault(x => x.Id == memberId.ToString());
            members.RemoveAll(x => x.Id == memberId.ToString());
            var meta = await GetTreeMeta(memberId);

            return new TreeResponseModel
            {
                Root = root,
                Members = members,
                Meta = new TreeMetaDto
                {
                    RootId = memberId,
                    Depth = depth,
                    MaxDepth = meta.MaxDepth,
                    TotalMembers = meta.TotalMembers,
                    Returned = members.Count
                }
            };
        }

        private TreeMemberDto MapTreeMember(IRecord record, INode node)
        {
            return new TreeMemberDto
            {
                Id = node.Properties["RegNo"].As<long>().ToString(),
                Name = $"{node.Properties["FirstName"].As<string>()} {node.Properties["LastName"].As<string>()}",
                RegistrationNumber = node.Properties["RegistrationNumber"].As<string>(),
                Status = "1",// node.Properties["Status"].As<string>(),
                ParentId = record["parentId"].As<object>()?.ToString(),
                Placement = record["placement"].As<object>()?.ToString(),
                Level = record["level"].As<int>(),
                LeftChildExists = record["leftChildExists"].As<bool>(),
                RightChildExists = record["rightChildExists"].As<bool>(),
                ChildrenCount = record["childrenCount"].As<int>(),
                HasChildren = record["hasChildren"].As<bool>(),
                IsEmpty = false
            };
        }

        private TreeMemberDto CreateEmptyNode(TreeMemberDto parent, string side)
        {
            return new TreeMemberDto
            {
                Id = $"empty-{parent.Id}-{side}",
                ParentId = parent.Id,
                Placement = side,
                Level = parent.Level + 1,
                IsEmpty = true,
                Name = "Add Member",
                Role = string.Empty,
                Package = string.Empty,
                Image = "/assets/add-user.png",
                Status = string.Empty,
                HasChildren = false,
                LeftChildExists = false,
                RightChildExists = false,
                ChildrenCount = 0
            };
        }

        private async Task<(int TotalMembers, int MaxDepth)> GetTreeMeta(int regno)
        {
            var metaCypher = @"MATCH path =
                (root:Member {
                    RegNo: $loginRootRegno, ClientId: $clientId
                })
                -[:PLACED*0..]->
                (currentRoot:Member {
                    RegNo: $regno, ClientId: $clientId
                })
                OPTIONAL MATCH currentPath =
                (currentRoot)-[:PLACED*]->
                (child:Member {ClientId: $clientId})
                RETURN
                    COUNT(DISTINCT child) + 1
                    AS totalMembers,
                    COALESCE(
                        MAX(length(currentPath)),
                        0
                    )
                    AS maxDepth
                ";
            var result = await _sessionProvider.ExecuteWriteAsync(metaCypher,
                new Dictionary<string, object>
                {
                    ["regno"] = regno,
                    ["loginRootRegno"] = 1
                }
            );

            var record = result.Single();
            return (
                record["totalMembers"].As<int>(),
                record["maxDepth"].As<int>()
            );
        }

        public async Task<UplineResponse> GetUplineMemberAsync(int memberId)
        {
            var query = @"MATCH path =(root:Member {RegNo:$rootRegno, ClientId: $clientId})-[:PLACED*0..]->(member:Member {RegNo:$regno, ClientId: $clientId})
                            RETURN path
                            ORDER BY length(path) DESC
                            LIMIT 1";

            var parameters = new Dictionary<string, object>
            {
                ["rootRegno"] = 1,
                ["regno"] = memberId
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, parameters);
            var results = records.FirstOrDefault();
            if (results == null) return new UplineResponse();

            // RegNo 1/2 are pre-seeded bootstrap placeholders (the internal tree root),
            // never real members -- must never appear in an upline shown to anyone.
            var path = results["path"].As<IPath>().Nodes
               .Where(x => !new[] { 1L, 2L }.Contains(x["RegNo"].As<long>()))
               .Select(x =>
               new UplineMemberDto
               {
                   Id = x["id"].As<string>(),
                   Name = $"{x["FirstName"].As<string>()} {x["LastName"].As<string>()}",
                   RegistrationNumber = x["RegistrationNumber"].As<string>()
               })
           .ToArray();

            return new UplineResponse { Path = path.ToList() };
        }

        public async Task<SubtreeResponse> GetMemberSubTreeAsync(int parentId, int depth)
        {
            var query = $@"
    MATCH (parent:Member {{
        RegNo: $parentId, ClientId: $clientId
    }})
    MATCH path =
    (parent)-[:PLACED*1..{depth}]->
    (child:Member {{ClientId: $clientId}})
    WHERE NOT child.RegNo IN [1, 2]
    OPTIONAL MATCH
    (child)-[:PLACED {{
        side:'L'
    }}]->(leftChild:Member {{ClientId: $clientId}})
    OPTIONAL MATCH
    (child)-[:PLACED {{
        side:'R'
    }}]->(rightChild:Member {{ClientId: $clientId}})
    RETURN
        child,
        parent.RegNo AS ParentId,
        length(path) AS Level,
        leftChild IS NOT NULL AS LeftChildExists,
        rightChild IS NOT NULL AS RightChildExists,
        (leftChild IS NOT NULL OR rightChild IS NOT NULL) AS HasChildren,
        COUNT {{
            (child)-[:PLACED]->(:Member {{ClientId: $clientId}})
        }} AS ChildrenCount
";
            var parameters = new Dictionary<string, object>
            {
                ["parentId"] = parentId,
            };
            var records = await _sessionProvider.ExecuteWriteAsync(query, parameters);
            var results = records.ToList();
            if (results == null) return new SubtreeResponse();
            var members = results.Select(r => new TreeMemberDto
            {
                Id = r["child"].As<INode>().Properties["RegNo"].As<string>(),
                Name = $"{r["child"].As<INode>().Properties["FirstName"].As<string>()} {r["child"].As<INode>().Properties["LastName"].As<string>()}",
                ParentId = r["ParentId"].As<string>(),
                Level = r["Level"].As<int>(),
                LeftChildExists = r["LeftChildExists"].As<bool>(),
                RightChildExists = r["RightChildExists"].As<bool>(),
                HasChildren = r["HasChildren"].As<bool>(),
                ChildrenCount = r["ChildrenCount"].As<int>()
            }).ToList();

            return new SubtreeResponse
            {
                ParentId = parentId.ToString(),
                Members = members,
                Depth = depth,
                Returned = members.Count
            };
        }

        public async Task<(int teamSize, int directMembers)> GetMemberTeamSizeAsync(Guid memberId)
        {
            var query = @"
               MATCH (member:Member {Id: $memberId, ClientId: $clientId})
OPTIONAL MATCH (member)-[:SPONSORED]->(direct:Member {ClientId: $clientId})
WITH member, COUNT(DISTINCT direct) AS directReferrals
OPTIONAL MATCH (member)-[:PLACED*]->(descendant:Member {ClientId: $clientId})
RETURN
    COUNT(DISTINCT descendant) AS teamSize,
    directReferrals
            ";
            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString()
            };
            var records = await _sessionProvider.ExecuteWriteAsync(query, parameters);
            var result = records.FirstOrDefault();
            if (result == null) return (0, 0);
            return (result["teamSize"].As<int>(), result["directReferrals"].As<int>());
        }

        public async Task<TeamSummaryDto> GetTeamSummaryAsync(Guid memberId, CancellationToken cancellationToken)
        {
            var query = @"
                MATCH (member:Member {Id: $memberId, ClientId: $clientId})
                OPTIONAL MATCH (member)-[:SPONSORED]->(direct:Member {ClientId: $clientId})
                WITH member, COUNT(DISTINCT direct) AS directCount
                OPTIONAL MATCH (member)-[:SPONSORED*2..]->(indirect:Member {ClientId: $clientId})
                RETURN
                    directCount AS directReferrals,
                    COUNT(DISTINCT indirect) AS indirectMembers,
                    directCount + COUNT(DISTINCT indirect) AS totalTeam
            ";

            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString()
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, parameters);
            var result = records.FirstOrDefault();

            if (result == null)
                return new TeamSummaryDto();

            return new TeamSummaryDto
            {
                DirectReferrals = result["directReferrals"].As<int>(),
                IndirectMembers = result["indirectMembers"].As<int>(),
                TotalTeam = result["totalTeam"].As<int>()
            };
        }

        public async Task<PagedResponse<TeamMemberDto>> GetDirectMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            var skip = (filter.Page - 1) * filter.PageSize;
            var sortBy = GetSortProperty(filter.SortBy!, "direct");

            var countQuery = @"
                MATCH (member:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(direct:Member {ClientId: $clientId})
                WHERE NOT direct.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                    toLower(direct.FirstName) CONTAINS toLower($search) OR
                    toLower(direct.LastName) CONTAINS toLower($search) OR
                    toString(direct.RegNo) CONTAINS $search OR
                    direct.RegistrationNumber CONTAINS $search)
                RETURN COUNT(direct) AS total
            ";

            var query = $@"
                MATCH (member:Member {{Id: $memberId, ClientId: $clientId}})-[:SPONSORED]->(direct:Member {{ClientId: $clientId}})
                WHERE NOT direct.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                    toLower(direct.FirstName) CONTAINS toLower($search) OR
                    toLower(direct.LastName) CONTAINS toLower($search) OR
                    toString(direct.RegNo) CONTAINS $search OR
                    direct.RegistrationNumber CONTAINS $search)
                OPTIONAL MATCH (sponsor:Member)-[:SPONSORED]->(direct)
                OPTIONAL MATCH (direct)-[:SPONSORED]->(dChild:Member {{ClientId: $clientId}})
                WITH direct, sponsor, COUNT(dChild) AS childCount
                RETURN
                    direct.RegNo AS regNo,
                    direct.RegistrationNumber AS loginId,
                    direct.FirstName AS firstName,
                    direct.LastName AS lastName,
                    direct.PhoneNumber AS mobile,
                    direct.EmailId AS email,
                    direct.ProfileFileId AS profileImage,
                    direct.JoiningDate AS joinDate,
                    1 AS level,
                    'Direct' AS relationshipType,
                    childCount > 0 AS hasChildren,
                    COALESCE(sponsor.FirstName + ' ' + sponsor.LastName, '') AS sponsorName,
                    sponsor.RegistrationNumber AS sponsorLoginId,
                    direct.Status AS status
                ORDER BY {sortBy} {GetSortDirection(filter.Descending)}
                SKIP $skip
                LIMIT $limit
            ";

            var countParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["search"] = filter.Search ?? string.Empty
            };

            var countRecords = await _sessionProvider.ExecuteWriteAsync(countQuery, countParams);
            var totalRecords = countRecords.FirstOrDefault()?["total"].As<int>() ?? 0;

            var queryParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["search"] = filter.Search ?? string.Empty,
                ["skip"] = skip,
                ["limit"] = filter.PageSize
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, queryParams);
            var items = records.Select(MapTeamMember).ToList();

            return CreatePagedResponse(items, filter.Page, filter.PageSize, totalRecords);
        }

        public async Task<PagedResponse<TeamMemberDto>> GetIndirectMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            var skip = (filter.Page - 1) * filter.PageSize;
            var sortBy = GetSortProperty(filter.SortBy!, "indirect");

            var countQuery = @"
                MATCH (member:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED*2..]->(indirect:Member {ClientId: $clientId})
                WHERE NOT indirect.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                    toLower(indirect.FirstName) CONTAINS toLower($search) OR
                    toLower(indirect.LastName) CONTAINS toLower($search) OR
                    toString(indirect.RegNo) CONTAINS $search OR
                    indirect.RegistrationNumber CONTAINS $search)
                RETURN COUNT(DISTINCT indirect) AS total
            ";

            var query = $@"
                MATCH path = (member:Member {{Id: $memberId, ClientId: $clientId}})-[:SPONSORED*2..]->(indirect:Member {{ClientId: $clientId}})
                WHERE NOT indirect.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                    toLower(indirect.FirstName) CONTAINS toLower($search) OR
                    toLower(indirect.LastName) CONTAINS toLower($search) OR
                    toString(indirect.RegNo) CONTAINS $search OR
                    indirect.RegistrationNumber CONTAINS $search)
                WITH indirect, length(path) AS level
                OPTIONAL MATCH (sponsor:Member)-[:SPONSORED]->(indirect)
                OPTIONAL MATCH (indirect)-[:SPONSORED]->(iChild:Member {{ClientId: $clientId}})
                WITH indirect, level, sponsor, COUNT(iChild) AS childCount
                RETURN
                    indirect.RegNo AS regNo,
                    indirect.RegistrationNumber AS loginId,
                    indirect.FirstName AS firstName,
                    indirect.LastName AS lastName,
                    indirect.PhoneNumber AS mobile,
                    indirect.EmailId AS email,
                    indirect.ProfileFileId AS profileImage,
                    indirect.JoiningDate AS joinDate,
                    level,
                    'Indirect' AS relationshipType,
                    childCount > 0 AS hasChildren,
                    COALESCE(sponsor.FirstName + ' ' + sponsor.LastName, '') AS sponsorName,
                    sponsor.RegistrationNumber AS sponsorLoginId,
                    indirect.Status AS status
                ORDER BY {sortBy} {GetSortDirection(filter.Descending)}
                SKIP $skip
                LIMIT $limit
            ";

            var countParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["search"] = filter.Search ?? string.Empty
            };

            var countRecords = await _sessionProvider.ExecuteWriteAsync(countQuery, countParams);
            var totalRecords = countRecords.FirstOrDefault()?["total"].As<int>() ?? 0;

            var queryParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["search"] = filter.Search ?? string.Empty,
                ["skip"] = skip,
                ["limit"] = filter.PageSize
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, queryParams);
            var items = records.Select(MapTeamMember).ToList();

            return CreatePagedResponse(items, filter.Page, filter.PageSize, totalRecords);
        }

        public async Task<PagedResponse<TeamMemberDto>> GetTotalTeamMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            var skip = (filter.Page - 1) * filter.PageSize;
            var sortBy = GetSortProperty(filter.SortBy!, "m");

            var countQuery = @"
                MATCH (member:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED*1..]->(m:Member {ClientId: $clientId})
                WHERE NOT m.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                    toLower(m.FirstName) CONTAINS toLower($search) OR
                    toLower(m.LastName) CONTAINS toLower($search) OR
                    toString(m.RegNo) CONTAINS $search OR
                    m.RegistrationNumber CONTAINS $search)
                RETURN COUNT(DISTINCT m) AS total
            ";

            var query = $@"
                MATCH path = (member:Member {{Id: $memberId, ClientId: $clientId}})-[:SPONSORED*1..]->(m:Member {{ClientId: $clientId}})
                WHERE NOT m.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                    toLower(m.FirstName) CONTAINS toLower($search) OR
                    toLower(m.LastName) CONTAINS toLower($search) OR
                    toString(m.RegNo) CONTAINS $search OR
                    m.RegistrationNumber CONTAINS $search)
                WITH m, length(path) AS level
                OPTIONAL MATCH (sponsor:Member)-[:SPONSORED]->(m)
                OPTIONAL MATCH (m)-[:SPONSORED]->(mChild:Member {{ClientId: $clientId}})
                WITH m, level, sponsor, COUNT(mChild) AS childCount
                RETURN
                    m.RegNo AS regNo,
                    m.RegistrationNumber AS loginId,
                    m.FirstName AS firstName,
                    m.LastName AS lastName,
                    m.PhoneNumber AS mobile,
                    m.EmailId AS email,
                    m.ProfileFileId AS profileImage,
                    m.JoiningDate AS joinDate,
                    level,
                    CASE WHEN level = 1 THEN 'Direct' ELSE 'Indirect' END AS relationshipType,
                    childCount > 0 AS hasChildren,
                    COALESCE(sponsor.FirstName + ' ' + sponsor.LastName, '') AS sponsorName,
                    sponsor.RegistrationNumber AS sponsorLoginId,
                    m.Status AS status
                ORDER BY {sortBy} {GetSortDirection(filter.Descending)}
                SKIP $skip
                LIMIT $limit
            ";

            var countParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["search"] = filter.Search ?? string.Empty
            };

            var countRecords = await _sessionProvider.ExecuteWriteAsync(countQuery, countParams);
            var totalRecords = countRecords.FirstOrDefault()?["total"].As<int>() ?? 0;

            var queryParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["search"] = filter.Search ?? string.Empty,
                ["skip"] = skip,
                ["limit"] = filter.PageSize
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, queryParams);
            var items = records.Select(MapTeamMember).ToList();

            return CreatePagedResponse(items, filter.Page, filter.PageSize, totalRecords);
        }

        public async Task<TeamHierarchyResponseDto> GetHierarchyAsync(Guid memberId, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString()
            };

            var rootQuery = @"
                MATCH (root:Member {Id: $memberId, ClientId: $clientId})
                CALL {
                    WITH root
                    OPTIONAL MATCH (root)-[:SPONSORED]->(d:Member {ClientId: $clientId})
                    WITH root, COUNT(d) AS directCount
                    OPTIONAL MATCH (root)-[:SPONSORED*1..]->(t:Member {ClientId: $clientId})
                    RETURN root AS rootMember, directCount, COUNT(DISTINCT t) AS totalCount
                }
                OPTIONAL MATCH (sponsor:Member)-[:SPONSORED]->(rootMember)
                RETURN
                    rootMember.RegNo AS regNo,
                    rootMember.RegistrationNumber AS loginId,
                    rootMember.FirstName AS firstName,
                    rootMember.LastName AS lastName,
                    rootMember.PhoneNumber AS mobile,
                    rootMember.EmailId AS email,
                    rootMember.ProfileFileId AS profileImage,
                    rootMember.JoiningDate AS joinDate,
                    rootMember.Status AS status,
                    directCount AS childrenCount,
                    directCount > 0 AS hasChildren,
                    directCount AS directReferralCount,
                    totalCount AS totalTeamCount,
                    COALESCE(sponsor.FirstName + ' ' + sponsor.LastName, '') AS sponsorName,
                    sponsor.RegistrationNumber AS sponsorLoginId
            ";

            var childrenQuery = @"
                MATCH (parent:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED]->(child:Member {ClientId: $clientId})
                WHERE NOT child.RegNo IN [1, 2]
                CALL {
                    WITH child, parent
                    OPTIONAL MATCH (child)-[:SPONSORED]->(gc:Member {ClientId: $clientId})
                    WITH child, parent, COUNT(gc) AS childrenCount
                    OPTIONAL MATCH (child)-[:SPONSORED*1..]->(desc:Member {ClientId: $clientId})
                    RETURN child AS childNode, parent AS parentNode, childrenCount, COUNT(DISTINCT desc) AS totalTeamCount
                }
                RETURN
                    childNode.RegNo AS regNo,
                    childNode.RegistrationNumber AS loginId,
                    childNode.FirstName AS firstName,
                    childNode.LastName AS lastName,
                    childNode.PhoneNumber AS mobile,
                    childNode.EmailId AS email,
                    childNode.ProfileFileId AS profileImage,
                    childNode.JoiningDate AS joinDate,
                    childrenCount,
                    childrenCount > 0 AS hasChildren,
                    childrenCount AS directReferralCount,
                    totalTeamCount,
                    COALESCE(parentNode.FirstName + ' ' + parentNode.LastName, '') AS sponsorName,
                    parentNode.RegistrationNumber AS sponsorLoginId,
                    childNode.Status AS status
                ORDER BY childNode.RegNo ASC
            ";

            var rootRecords = await _sessionProvider.ExecuteWriteAsync(rootQuery, parameters);
            var rootRecord = rootRecords.FirstOrDefault();

            if (rootRecord == null)
                return new TeamHierarchyResponseDto();

            var childrenRecords = await _sessionProvider.ExecuteWriteAsync(childrenQuery, parameters);

            return new TeamHierarchyResponseDto
            {
                CurrentMember = MapHierarchyNode(rootRecord),
                Children = childrenRecords.Select(MapHierarchyNode).ToList()
            };
        }

        public async Task<IReadOnlyList<TeamHierarchyNodeDto>> GetChildrenAsync(long parentRegNo, CancellationToken cancellationToken)
        {
            var query = @"
                MATCH (parent:Member {RegNo: $parentRegNo, ClientId: $clientId})-[:SPONSORED]->(child:Member {ClientId: $clientId})
                WHERE NOT child.RegNo IN [1, 2]
                CALL {
                    WITH child, parent
                    OPTIONAL MATCH (child)-[:SPONSORED]->(gc:Member {ClientId: $clientId})
                    WITH child, parent, COUNT(gc) AS childrenCount
                    OPTIONAL MATCH (child)-[:SPONSORED*1..]->(desc:Member {ClientId: $clientId})
                    RETURN child AS childNode, parent AS parentNode, childrenCount, COUNT(DISTINCT desc) AS totalTeamCount
                }
                RETURN
                    childNode.RegNo AS regNo,
                    childNode.RegistrationNumber AS loginId,
                    childNode.FirstName AS firstName,
                    childNode.LastName AS lastName,
                    childNode.PhoneNumber AS mobile,
                    childNode.EmailId AS email,
                    childNode.ProfileFileId AS profileImage,
                    childNode.JoiningDate AS joinDate,
                    childrenCount,
                    childrenCount > 0 AS hasChildren,
                    childrenCount AS directReferralCount,
                    totalTeamCount,
                    COALESCE(parentNode.FirstName + ' ' + parentNode.LastName, '') AS sponsorName,
                    parentNode.RegistrationNumber AS sponsorLoginId,
                    childNode.Status AS status
                ORDER BY childNode.RegNo ASC
            ";

            var parameters = new Dictionary<string, object>
            {
                ["parentRegNo"] = parentRegNo
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, parameters);
            return records.Select(MapHierarchyNode).ToList();
        }

        public async Task<IReadOnlyList<LevelSummaryDto>> GetLevelSummariesAsync(Guid memberId, CancellationToken cancellationToken)
        {
            var query = @"
                MATCH path = (me:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED*1..]->(member:Member {ClientId: $clientId})
                WHERE NOT member.RegNo IN [1, 2]
                RETURN length(path) AS level,
                       COUNT(DISTINCT member) AS memberCount,
                       COUNT(DISTINCT CASE WHEN member.Status = 1 THEN member END) AS activeMembers,
                       COUNT(DISTINCT CASE WHEN member.Status = 0 THEN member END) AS inactiveMembers
                ORDER BY level
            ";

            var parameters = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString()
            };

            var records = await _sessionProvider.ExecuteWriteAsync(query, parameters);

            return records.Select(record => new LevelSummaryDto
            {
                Level = record["level"].As<int>(),
                MemberCount = record["memberCount"].As<int>(),
                ActiveMembers = record["activeMembers"].As<int>(),
                InactiveMembers = record["inactiveMembers"].As<int>()
            }).ToList();
        }

        public async Task<PagedResponse<LevelMemberDto>> GetLevelMembersAsync(Guid memberId, int level, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            var skip = (filter.Page - 1) * filter.PageSize;
            var sortBy = GetSortProperty(filter.SortBy!, "member");
            var sortDir = GetSortDirection(filter.Descending);

            var countQuery = @"
                MATCH path = (me:Member {Id: $memberId, ClientId: $clientId})-[:SPONSORED*1..]->(member:Member {ClientId: $clientId})
                WHERE length(path) = $level
                  AND NOT member.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                       toLower(member.FirstName) CONTAINS toLower($search) OR
                       toLower(member.LastName) CONTAINS toLower($search) OR
                       toString(member.RegNo) CONTAINS $search OR
                       member.RegistrationNumber CONTAINS $search OR
                       member.PhoneNumber CONTAINS $search)
                RETURN COUNT(DISTINCT member) AS total
            ";

            var dataQuery = $@"
                MATCH path = (me:Member {{Id: $memberId, ClientId: $clientId}})-[:SPONSORED*1..]->(member:Member {{ClientId: $clientId}})
                WHERE length(path) = $level
                  AND NOT member.RegNo IN [1, 2]
                  AND ($search IS NULL OR $search = '' OR
                       toLower(member.FirstName) CONTAINS toLower($search) OR
                       toLower(member.LastName) CONTAINS toLower($search) OR
                       toString(member.RegNo) CONTAINS $search OR
                       member.RegistrationNumber CONTAINS $search OR
                       member.PhoneNumber CONTAINS $search)
                WITH DISTINCT member
                CALL {{
                    WITH member
                    OPTIONAL MATCH (member)-[:SPONSORED]->(direct:Member {{ClientId: $clientId}})
                    RETURN COUNT(direct) AS directReferralCount
                }}
                CALL {{
                    WITH member
                    OPTIONAL MATCH (member)-[:SPONSORED*1..]->(team:Member {{ClientId: $clientId}})
                    RETURN COUNT(DISTINCT team) AS totalTeamCount
                }}
                RETURN
                    member.RegNo AS regNo,
                    member.RegistrationNumber AS loginId,
                    member.FirstName AS firstName,
                    member.LastName AS lastName,
                    member.PhoneNumber AS mobile,
                    member.EmailId AS email,
                    member.ProfileFileId AS profileImage,
                    member.JoiningDate AS joinDate,
                    member.Status AS status,
                    $level AS level,
                    directReferralCount,
                    totalTeamCount
                ORDER BY {sortBy} {sortDir}
                SKIP $skip
                LIMIT $limit
            ";

            var countParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["level"] = level,
                ["search"] = filter.Search ?? string.Empty
            };

            var countRecords = await _sessionProvider.ExecuteWriteAsync(countQuery, countParams);
            var totalRecords = countRecords.FirstOrDefault()?["total"].As<int>() ?? 0;

            var dataParams = new Dictionary<string, object>
            {
                ["memberId"] = memberId.ToString(),
                ["level"] = level,
                ["search"] = filter.Search ?? string.Empty,
                ["skip"] = skip,
                ["limit"] = filter.PageSize
            };

            var records = await _sessionProvider.ExecuteWriteAsync(dataQuery, dataParams);
            var items = records.Select(MapLevelMember).ToList();

            return new PagedResponse<LevelMemberDto>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)filter.PageSize),
                Items = items
            };
        }

        private static LevelMemberDto MapLevelMember(IRecord record)
        {
            return new LevelMemberDto
            {
                RegNo = record["regNo"].As<long>(),
                LoginId = record["loginId"].As<string>(),
                Name = $"{record["firstName"].As<string>()} {record["lastName"].As<string>()}",
                Mobile = record["mobile"].As<string>(),
                Email = record["email"].As<string>(),
                ProfileImage = record["profileImage"].As<string>(),
                JoinDate = record["joinDate"].As<DateTime?>(),
                Status = record["status"].As<int>(),
                Level = record["level"].As<int>(),
                DirectReferralCount = record["directReferralCount"].As<int>(),
                TotalTeamCount = record["totalTeamCount"].As<int>()
            };
        }

        private static TeamHierarchyNodeDto MapHierarchyNode(IRecord record)
        {
            return new TeamHierarchyNodeDto
            {
                RegNo = record["regNo"].As<long>(),
                LoginId = record["loginId"].As<string>(),
                Name = $"{record["firstName"].As<string>()} {record["lastName"].As<string>()}",
                Mobile = record["mobile"].As<string>(),
                Email = record["email"].As<string>(),
                ProfileImage = record["profileImage"].As<string>(),
                SponsorName = record["sponsorName"].As<string>(),
                SponsorLoginId = record["sponsorLoginId"].As<string>(),
                JoinDate = record["joinDate"].As<DateTime?>(),
                HasChildren = record["hasChildren"].As<bool>(),
                ChildrenCount = record["childrenCount"].As<int>(),
                DirectReferralCount = record["directReferralCount"].As<int>(),
                TotalTeamCount = record["totalTeamCount"].As<int>(),
                Status = record["status"].As<int>()
            };
        }

        private static TeamMemberDto MapTeamMember(IRecord record)
        {
            return new TeamMemberDto
            {
                RegNo = record["regNo"].As<long>(),
                LoginId = record["loginId"].As<string>(),
                Name = $"{record["firstName"].As<string>()} {record["lastName"].As<string>()}",
                Mobile = record["mobile"].As<string>(),
                Email = record["email"].As<string>(),
                ProfileImage = record["profileImage"].As<string>(),
                SponsorName = record["sponsorName"].As<string>(),
                SponsorLoginId = record["sponsorLoginId"].As<string>(),
                JoinDate = record["joinDate"].As<DateTime?>(),
                Level = record["level"].As<int>(),
                RelationshipType = record["relationshipType"].As<string>(),
                HasChildren = record["hasChildren"].As<bool>(),
                Status = record["status"].As<int>()
            };
        }

        private static string GetSortProperty(string colName, string prefix)
        {
            return colName?.ToLowerInvariant() switch
            {
                "regno" => $"{prefix}.RegNo",
                "loginid" => $"{prefix}.RegistrationNumber",
                "name" => $"{prefix}.FirstName",
                "joindate" => $"{prefix}.JoiningDate",
                _ => $"{prefix}.RegNo"
            };
        }

        private static string GetSortDirection(bool descending)
        {
            return descending ? "DESC" : "ASC";
        }

        private static PagedResponse<TeamMemberDto> CreatePagedResponse(
            List<TeamMemberDto> items, int pageIndex, int pageSize, int totalRecords)
        {
            return new PagedResponse<TeamMemberDto>
            {
                Page = pageIndex,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Items = items
            };
        }
    }
}
