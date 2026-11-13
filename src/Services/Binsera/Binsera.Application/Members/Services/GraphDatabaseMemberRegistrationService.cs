using Binsera.Application.Members.Models;
using Binsera.Core;
using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Neo4J.Configuration;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Neo4j.Driver;

namespace Binsera.Application.Members.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class GraphDatabaseMemberRegistrationService(
        SynchronizationService synchronizationService,
        ClientAwareSessionProvider sessionProvider,
        IUnitOfWork unitOfWork) : IGraphDatabaseMemberRegistrationService
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        public async Task UpdateMember(long memberId)
        {
            var member = _memberRepository.TableNoTracking.Where(m => m.Id == memberId).First();
            var graphId = member.MemberGraphId.ToString();

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graphId,
                ["title"] = member.Title ?? string.Empty,
                ["firstName"] = member.FName ?? string.Empty,
                ["lastName"] = member.LName ?? string.Empty,
                ["emailId"] = member.EmailID ?? string.Empty,
                ["phoneNumber"] = member.Mobile ?? string.Empty,
                ["state"] = member.OState ?? string.Empty,
                ["pinCode"] = member.PinCode ?? string.Empty,
                ["profileFileId"] = member.ProfileImage ?? string.Empty
            };

            await sessionProvider.ExecuteWriteAsync(@"
                MATCH (n:Member {Id: $graphId, ClientId: $clientId})
                SET n.Title = $title,
                    n.FirstName = $firstName,
                    n.LastName = $lastName,
                    n.EmailId = $emailId,
                    n.PhoneNumber = $phoneNumber,
                    n.State = $state,
                    n.PinCode = $pinCode,
                    n.ProfileFileId = $profileFileId",
                parameters);
        }

        public async Task UpdateMemberProfileImage(long memberId, string profileFileId)
        {
            var member = _memberRepository.TableNoTracking.Where(m => m.Id == memberId).First();
            var graphId = member.MemberGraphId.ToString();

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graphId,
                ["profileFileId"] = profileFileId
            };

            await sessionProvider.ExecuteWriteAsync(@"
                MATCH (n:Member {Id: $graphId, ClientId: $clientId})
                SET n.ProfileFileId = $profileFileId",
                parameters);
        }

        public async Task UpdateMemberStatus(Guid memberGraphId, int? confPayNo)
        {
            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = memberGraphId.ToString(),
                ["status"] = confPayNo ?? 0
            };

            await sessionProvider.ExecuteWriteAsync(@"
                MATCH (n:Member {Id: $graphId, ClientId: $clientId})
                SET n.Status = $status",
                parameters);
        }

        public async Task RegisterMember(long memberId)
        {
            var member = _memberRepository.TableNoTracking.Where(m => m.Id == memberId).First();

            if (member.IntroRegNo == 0)
            {
                // ROOT USER (no introducer)
                var (depth, path) = GetRootDepthAndPath(member.MemberGraphId);
                await CreateUserNode(member, "", depth, path, null, true);
                return;
            }
            var introMember = _memberRepository.TableNoTracking.Where(m => m.RegNo == member.IntroRegNo).First();
            var introMemberId = introMember.MemberGraphId;

            var placementParentId = await GetPlacementParentId(introMemberId.ToString(), member.IntroSide?.ToString() ?? string.Empty);
            if (placementParentId == string.Empty)
            {
                var (depth, path) = GetRootDepthAndPath(member.MemberGraphId);
                await CreateUserNode(member, introMemberId.ToString(), depth, path, null, true);
                return;
            }
            var pathAndDepth = await GetDepthAndPath(placementParentId, member.MemberGraphId);
            await CreateUserNode(member, introMemberId.ToString(), pathAndDepth.Depth, pathAndDepth.Path, placementParentId, false);
        }
        private async Task CreateUserNode(Member member, string sponsorId, int depth, string path, string? placementParentId, bool isRootUser)
        {
            var memberUser = new UserNode
            {
                Id = member.MemberGraphId.ToString(),
                EmailId = member.EmailID,
                FirstName = member.FName,
                LastName = member.LName,
                Depth = depth,
                Path = path,
                JoiningDate = member.RegDate ?? DateTime.Now,
                MiddleName = string.Empty,
                PhoneNumber = member.Mobile,
                PinCode = member.PinCode,
                PlacementParentId = placementParentId,
                PlacementSide = member.IntroSide?.ToString() ?? string.Empty,
                RegNo = member.RegNo ?? 0,
                RegistrationNumber = member.LoginID,
                SponsorId = sponsorId,
                State = member.OState,
                Status = member.ConfPayNo.HasValue && member.ConfPayNo.Value == 1 ? 1 : 0,
                Title = member.Title,
                RankId = 5,
                ProfileFileId = member.ProfileImage ?? string.Empty,
                ClientId = CompanySetting.Default.ClientId,
            };
            var graphNode = new GraphEvent
            {
                EntityType = "Member",
                Entity = memberUser,
                EntityId = member.MemberGraphId.ToString(),

            };
            if (!isRootUser)
            {
                graphNode.Relations = new List<RelationDescriptor>
                {
                    new RelationDescriptor
                    {
                        RelationshipType = "SPONSORED",
                        TargetEntityType = "Member",
                        TargetEntityId = sponsorId.ToString(),
                        Direction=RelationDirection.Incoming
                    }
                };
                await synchronizationService.Sync(graphNode);

                await CreatePlacementRelation(placementParentId, member.MemberGraphId.ToString(), memberUser.PlacementSide);
            }
            else
                await synchronizationService.Sync(graphNode);
        }
        private async Task<string> GetPlacementParentId(string sponsorId, string side)
        {
            // FIND FIRST EMPTY LEFT/RIGHT POSITION

            var query = @"MATCH (root:Member {
                Id:$sponsorId, ClientId: $clientId
            })
            OPTIONAL MATCH path =
            (root)-[:PLACED*]->(node)
            WITH collect(root) + collect(node) AS allNodes
            UNWIND allNodes AS node
            WITH DISTINCT node
            WHERE NOT EXISTS {
                MATCH (node)-[:PLACED {
                    side:$side
                }]->(:Member {ClientId: $clientId})
            }
            RETURN node.Id as Id
            LIMIT 1";


            var result = await sessionProvider.ExecuteWriteAsync(
                query,
                new Dictionary<string, object>
                {
                    ["sponsorId"] = sponsorId,
                    ["side"] = side
                });

            var record = result.FirstOrDefault();

            if (record == null)
            {
                return string.Empty;
            }

            return record["Id"].As<string>();
        }
        private async Task<(int Depth, string Path)> GetDepthAndPath(string placementParentId, Guid userId)
        {
            // GET PARENT NODE

            var result = await sessionProvider.ExecuteWriteAsync(@"
                MATCH (u:Member {
                    Id:$parentId, ClientId: $clientId
                })
                RETURN
            u.Depth AS depth,
            u.Path AS path",
            new Dictionary<string, object>
            {
                ["parentId"] = placementParentId,
            });

            var record =
                 result.First();

            // PARENT DEPTH

            var parentDepth =
                record["depth"].As<int>();

            // PARENT PATH

            var parentPath =
                record["path"].As<string>();

            // NEW USER DEPTH

            var newDepth =
                parentDepth + 1;

            // NEW USER PATH

            var newPath =
                $"{parentPath}/{userId}";

            return (newDepth, newPath);
        }
        private (int Depth, string Path) GetRootDepthAndPath(Guid userId)
        {
            return (
                0,
                $"/{userId}"
            );
        }
        // CREATE PLACEMENT RELATION
        private async Task CreatePlacementRelation(
            string placementParentId,
            string userId,
            string side)
        {
            await sessionProvider.ExecuteWriteAsync(@"

            MATCH (parent:Member {
                Id:$parentId, ClientId: $clientId
            })
            MATCH (user:Member {
                Id:$userId, ClientId: $clientId
            })
            WHERE NOT EXISTS { MATCH (:Member)-[:PLACED]->(user) }
            CREATE
            (parent)-[:PLACED {
                side:$side
            }]->(user)",
            new Dictionary<string, object>
            {
                ["parentId"] = placementParentId,
                ["userId"] = userId,
                ["side"] = side
            });
        }
    }
}
