using Binsera.Application.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;

namespace Binsera.Application.Members.Team
{
    [AutoRegister(includeInterfaces: true)]
    public class TeamService : ITeamService
    {
        private readonly IGraphDatabaseMemberQueryService _graphQueryService;

        public TeamService(IGraphDatabaseMemberQueryService graphQueryService)
        {
            _graphQueryService = graphQueryService;
        }

        public async Task<TeamSummaryDto> GetTeamSummaryAsync(Guid memberId, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetTeamSummaryAsync(memberId, cancellationToken);
        }

        public async Task<PagedResponse<TeamMemberDto>> GetDirectMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetDirectMembersAsync(memberId, filter, cancellationToken);
        }

        public async Task<PagedResponse<TeamMemberDto>> GetIndirectMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetIndirectMembersAsync(memberId, filter, cancellationToken);
        }

        public async Task<PagedResponse<TeamMemberDto>> GetTotalTeamMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetTotalTeamMembersAsync(memberId, filter, cancellationToken);
        }

        public async Task<TeamHierarchyResponseDto> GetHierarchyAsync(Guid memberId, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetHierarchyAsync(memberId, cancellationToken);
        }

        public async Task<IReadOnlyList<TeamHierarchyNodeDto>> GetChildrenAsync(long parentRegNo, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetChildrenAsync(parentRegNo, cancellationToken);
        }

        public async Task<IReadOnlyList<LevelSummaryDto>> GetLevelSummariesAsync(Guid memberId, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetLevelSummariesAsync(memberId, cancellationToken);
        }

        public async Task<PagedResponse<LevelMemberDto>> GetLevelMembersAsync(Guid memberId, int level, TeamFilterModel filter, CancellationToken cancellationToken)
        {
            return await _graphQueryService.GetLevelMembersAsync(memberId, level, filter, cancellationToken);
        }
    }
}
