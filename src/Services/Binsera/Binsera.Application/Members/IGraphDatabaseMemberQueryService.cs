using Binsera.Application.Members.Models;

namespace Binsera.Application.Members
{
    public interface IGraphDatabaseMemberQueryService
    {
        Task<TreeResponseModel> GetMemberTreeAsync(int memberId, int depth);
        Task<UplineResponse> GetUplineMemberAsync(int memberId);
        Task<SubtreeResponse> GetMemberSubTreeAsync(int parentId, int depth);
        Task<(int teamSize, int directMembers)> GetMemberTeamSizeAsync(Guid memberId);
        Task<TeamSummaryDto> GetTeamSummaryAsync(Guid memberId, CancellationToken cancellationToken);
        Task<PagedResponse<TeamMemberDto>> GetDirectMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken);
        Task<PagedResponse<TeamMemberDto>> GetIndirectMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken);
        Task<PagedResponse<TeamMemberDto>> GetTotalTeamMembersAsync(Guid memberId, TeamFilterModel filter, CancellationToken cancellationToken);
        Task<TeamHierarchyResponseDto> GetHierarchyAsync(Guid memberId, CancellationToken cancellationToken);
        Task<IReadOnlyList<TeamHierarchyNodeDto>> GetChildrenAsync(long parentRegNo, CancellationToken cancellationToken);
        Task<IReadOnlyList<LevelSummaryDto>> GetLevelSummariesAsync(Guid memberId, CancellationToken cancellationToken);
        Task<PagedResponse<LevelMemberDto>> GetLevelMembersAsync(Guid memberId, int level, TeamFilterModel filter, CancellationToken cancellationToken);
    }
}
