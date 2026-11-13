using Binsera.Core.AdminDashboard.Models;

namespace Binsera.Core.AdminDashboard.Contracts
{
    public interface IAdminDashboardService
    {
        Task<ReconciliationResponse> GetReconciliationAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
        Task<DashboardQueueResponse> GetQueueAsync(CancellationToken cancellationToken);
        Task<DashboardPositionResponse> GetPositionAsync(CancellationToken cancellationToken);
        Task<JoiningsResponse> GetJoiningsAsync(int days, CancellationToken cancellationToken);
        Task<DashboardLevelDistributionResponse> GetLevelDistributionAsync(CancellationToken cancellationToken);
        Task<TreeHealthResponse> GetTreeHealthAsync(CancellationToken cancellationToken);
        Task<StuckMembersResponse> GetStuckMembersAsync(int minDays, int limit, CancellationToken cancellationToken);
    }
}
