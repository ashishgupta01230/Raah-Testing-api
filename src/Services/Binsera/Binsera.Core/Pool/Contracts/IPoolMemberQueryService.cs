using Binsera.Core.Pool.Models;

namespace Binsera.Core.Pool.Contracts
{
    /// <summary>
    /// Read-only queries a member runs against their own pool standing. Every method takes the
    /// caller's own RegNo, resolved from the JWT by the controller -- never a target id from the
    /// request -- and for tree/team lookups additionally validates the requested root is the
    /// caller themself or someone in their downline (PART 16 of the UI design doc).
    /// </summary>
    public interface IPoolMemberQueryService
    {
        Task<MemberSummaryResponse> GetSummaryAsync(long regNo, CancellationToken cancellationToken);

        Task<List<ActivityItem>> GetActivityAsync(long regNo, int limit, CancellationToken cancellationToken);

        Task<List<TreeNodeResponse>> GetTreeAsync(long regNo, long rootRegNo, int depth, CancellationToken cancellationToken);

        /// <summary>
        /// The pool tree in the shape the existing vis-network component consumes.
        /// <paramref name="isAdmin"/> lifts the self-or-downline restriction, since the admin tree
        /// pages legitimately view any member; for a plain member it stays enforced.
        /// </summary>
        Task<BinaryTreeResponse> GetBinaryTreeAsync(
            long callerRegNo, bool isAdmin, long rootRegNo, int depth, CancellationToken cancellationToken);

        Task<BinarySubtreeResponse> GetBinarySubtreeAsync(
            long callerRegNo, bool isAdmin, long parentRegNo, int depth, CancellationToken cancellationToken);

        Task<BinaryMemberDetailResponse?> GetBinaryMemberAsync(
            long callerRegNo, bool isAdmin, long regNo, CancellationToken cancellationToken);

        /// <summary>Searches by member code or name, restricted to the caller's own downline.</summary>
        Task<BinarySearchResponse> SearchBinaryMembersAsync(
            long callerRegNo, bool isAdmin, string query, int limit, CancellationToken cancellationToken);

        Task<BinaryUplineResponse> GetBinaryUplineAsync(
            long callerRegNo, bool isAdmin, long regNo, CancellationToken cancellationToken);

        Task<TeamListResponse> GetTeamAsync(
            long regNo, string? search, int? level, int page, int pageSize, CancellationToken cancellationToken);

        Task<IncomeStatementResponse> GetIncomeAsync(long regNo, CancellationToken cancellationToken);

        Task<List<WalletLedgerEntry>> GetWalletLedgerAsync(
            long regNo, bool mainWallet, DateTime? from, DateTime? to, CancellationToken cancellationToken);

        Task<List<UpgradeHistoryEntry>> GetUpgradeHistoryAsync(long regNo, CancellationToken cancellationToken);
    }
}
