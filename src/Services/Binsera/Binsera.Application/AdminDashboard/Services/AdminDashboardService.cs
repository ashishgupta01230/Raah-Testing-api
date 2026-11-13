using Binsera.Core.AdminDashboard.Contracts;
using Binsera.Core.AdminDashboard.Models;
using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Enums;
using Binsera.Core.OutBox.Domain;
using Binsera.Core.Pool;
using Binsera.Core.Pool.Domain;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.AdminDashboard.Services
{
    /// <summary>
    /// Backs Part 3 (docs/plan/Admin_Dashboard_And_Reports_Plan.md) -- the reconciliation-first admin
    /// dashboard. Every figure here is a SUM/COUNT over tables the pool engine already writes
    /// (PoolWalletTransaction, MemberLevel, Member_Account); nothing is a second,
    /// separately-maintained ledger, which is what makes the reconciliation check mean something.
    /// </summary>
    [AutoRegister(includeInterfaces: true)]
    public class AdminDashboardService(BinseraDbContext dbContext) : IAdminDashboardService
    {
        /// <summary>
        /// Spec Part 3.7: RISK at 45+ days with zero earned, WATCH at 30-44 days, NORMAL under 30.
        /// Named constants rather than buried literals so a future config move is a one-line change,
        /// not a hunt through the query.
        /// </summary>
        private const int RiskDays = 45;
        private const int WatchDays = 30;

        public async Task<ReconciliationResponse> GetReconciliationAsync(
            DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            var toExclusive = to.Date.AddDays(1);

            // The window's own joinings, for the "N new members today" headline -- informational,
            // not what the balance check runs against (see below for why). PPositionM > 0 excludes
            // the X1-X7 seed accounts, which live in member_Master with a RegDate too but never
            // joined the pool -- counting them inflated newMembers and, worse, poolContribution,
            // since seeds contribute nothing.
            var newMembersInWindow = await dbContext.Set<Member>().AsNoTracking()
                .CountAsync(x => x.PPositionM > 0 && x.RegDate >= from.Date && x.RegDate < toExclusive, cancellationToken);

            var joiningContribution = await dbContext.Set<PoolLevelMaster>().AsNoTracking()
                .Where(x => x.LevelNo == 0)
                .Select(x => (decimal?)x.ContributionPerMember)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m;

            // The balance check is cumulative-to-date, not a same-day slice. A level needs several
            // members' contributions to complete (2, 4, 8, ...), so a rupee credited today can sit in
            // an Upgrade wallet for days before a later join completes that level and releases it to
            // Main or Company -- same-day inflow does not equal same-day outflow in the general case,
            // only over the ledger's whole history. Verified against the live pool: 19 members x
            // Rs.15 = Rs.285, and Main(10) + Company(215) + the Rs.60 still sitting in Upgrade wallets
            // also sums to Rs.285 -- the invariant holds cumulatively, not per day.
            var totalMembersToDate = await dbContext.Set<Member>().AsNoTracking()
                .CountAsync(x => x.PPositionM > 0 && x.RegDate < toExclusive, cancellationToken);
            var cumulativeIn = totalMembersToDate * joiningContribution;

            var toMembersToDate = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Main && x.OccurredOn < toExclusive)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit))
                .FirstOrDefaultAsync(cancellationToken);

            var toCompanyToDate = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Company && x.OccurredOn < toExclusive)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit))
                .FirstOrDefaultAsync(cancellationToken);

            var upgradeBalanceToDate = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Upgrade && x.OccurredOn < toExclusive)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit) - g.Sum(x => x.Debit))
                .FirstOrDefaultAsync(cancellationToken);

            // What every rupee that has ever entered the pool should sum to: paid to members, sent to
            // the company, or still legitimately in flight in an Upgrade wallet. Non-zero here means
            // money the engine cannot account for -- a real bug, not a level still filling.
            var unallocated = cumulativeIn - toMembersToDate - toCompanyToDate - upgradeBalanceToDate;
            var totalOut = toMembersToDate + toCompanyToDate + upgradeBalanceToDate;
            var difference = cumulativeIn - totalOut;

            return new ReconciliationResponse
            {
                From = from.Date,
                To = to.Date,
                Inflow = new ReconciliationInflow
                {
                    NewMembers = newMembersInWindow,
                    JoiningCollected = 0,
                    ProductAndMargin = 0,
                    PoolContribution = cumulativeIn
                },
                Outflow = new ReconciliationOutflow
                {
                    ToMemberMainWallets = toMembersToDate,
                    ToCompanyWallet = toCompanyToDate,
                    Unallocated = unallocated,
                    TotalOut = totalOut
                },
                IsBalanced = difference == 0,
                Difference = difference
            };
        }

        public async Task<DashboardQueueResponse> GetQueueAsync(CancellationToken cancellationToken)
        {
            var kycPending = await dbContext.Set<MemberDocument>().AsNoTracking()
                .Where(x => x.Status == MemberDocumentStatus.Pending)
                .Select(x => x.MemberId)
                .Distinct()
                .CountAsync(cancellationToken);

            var failedJobs = await dbContext.Set<OutboxMessage>().AsNoTracking()
                .CountAsync(x => x.DeadLetter, cancellationToken);

            return new DashboardQueueResponse
            {
                WithdrawalsPending = 0,
                WithdrawalsPendingAmount = 0,
                KycPending = kycPending,
                BankVerifyPending = 0,
                FailedJobs = failedJobs,
                OldestPendingWithdrawalDays = 0
            };
        }

        public async Task<DashboardPositionResponse> GetPositionAsync(CancellationToken cancellationToken)
        {
            var memberLiability = await dbContext.Set<MemberAccount>().AsNoTracking()
                .Where(x => x.TrStatus == 1)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit) - g.Sum(x => x.Debit))
                .FirstOrDefaultAsync(cancellationToken);

            var companyWallet = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Company)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit) - g.Sum(x => x.Debit))
                .FirstOrDefaultAsync(cancellationToken);

            var stuckInLevelPools = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Upgrade)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit) - g.Sum(x => x.Debit))
                .FirstOrDefaultAsync(cancellationToken);

            return new DashboardPositionResponse
            {
                MemberLiability = memberLiability,
                CompanyWallet = companyWallet,
                TotalWithdrawn = 0,
                StuckInLevelPools = stuckInLevelPools,
                AsOn = DateTime.Now
            };
        }

        public async Task<JoiningsResponse> GetJoiningsAsync(int days, CancellationToken cancellationToken)
        {
            var window = Math.Clamp(days, 1, 90);
            var today = DateOnly.FromDateTime(DateTime.Now);
            var start = today.AddDays(-(window - 1));
            var startDateTime = start.ToDateTime(TimeOnly.MinValue);

            // PPositionM > 0 excludes the X1-X7 seed accounts, which carry a RegDate but never
            // actually joined the pool.
            var rows = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM > 0 && x.RegDate >= startDateTime)
                .Select(x => x.RegDate!.Value)
                .ToListAsync(cancellationToken);

            var byDate = rows.GroupBy(x => DateOnly.FromDateTime(x))
                .ToDictionary(g => g.Key, g => g.Count());

            var series = Enumerable.Range(0, window)
                .Select(i => start.AddDays(i))
                .Select(d => new JoiningDayCount { Date = d, Count = byDate.GetValueOrDefault(d, 0) })
                .ToList();

            var total = series.Sum(x => x.Count);
            var avg = window > 0 ? Math.Round((decimal)total / window, 1) : 0m;

            var half = window / 2;
            var trend = "FLAT";
            if (half > 0)
            {
                var firstHalfAvg = series.Take(half).Average(x => x.Count);
                var secondHalfAvg = series.Skip(window - half).Average(x => x.Count);
                trend = secondHalfAvg > firstHalfAvg ? "UP" : secondHalfAvg < firstHalfAvg ? "DOWN" : "FLAT";
            }

            return new JoiningsResponse
            {
                Series = series,
                Total = total,
                DailyAverage = avg,
                Trend = trend
            };
        }

        public async Task<DashboardLevelDistributionResponse> GetLevelDistributionAsync(CancellationToken cancellationToken)
        {
            var completedByRegNo = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => x.MlActive == true)
                .GroupBy(x => x.RegNo)
                .Select(g => new { RegNo = g.Key, Completed = g.Count() })
                .ToDictionaryAsync(x => x.RegNo, x => x.Completed, cancellationToken);

            var memberRegNos = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM != null && x.PPositionM > 0)
                .Select(x => x.RegNo!.Value)
                .ToListAsync(cancellationToken);

            var buckets = new int[8];
            foreach (var regNo in memberRegNos)
            {
                var level = Math.Clamp(completedByRegNo.GetValueOrDefault(regNo, 0), 0, 7);
                buckets[level]++;
            }

            var total = memberRegNos.Count;
            var levels = Enumerable.Range(1, 7)
                .Select(level => new LevelDistributionRow
                {
                    Level = level,
                    Count = buckets[level],
                    Percent = total > 0 ? Math.Round(buckets[level] * 100m / total, 1) : 0m
                })
                .ToList();

            return new DashboardLevelDistributionResponse { TotalMembers = total, Levels = levels };
        }

        public async Task<TreeHealthResponse> GetTreeHealthAsync(CancellationToken cancellationToken)
        {
            var maxPosition = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM != null && x.PPositionM > 0)
                .MaxAsync(x => (long?)x.PPositionM, cancellationToken) ?? 0;

            if (maxPosition == 0)
                return new TreeHealthResponse { TreeDepth = 0, LastRow = new TreeHealthLastRow() };

            var treeDepth = PoolTreePosition.DepthOf(maxPosition);
            var lastRowStart = 1L << treeDepth;
            var lastRowCapacity = 1L << treeDepth;

            var filledInLastRow = await dbContext.Set<Member>().AsNoTracking()
                .CountAsync(x => x.PPositionM >= lastRowStart && x.PPositionM < lastRowStart + lastRowCapacity, cancellationToken);

            var bottomTwoStart = treeDepth == 0 ? lastRowStart : 1L << (treeDepth - 1);
            var totalMembers = await dbContext.Set<Member>().AsNoTracking()
                .CountAsync(x => x.PPositionM != null && x.PPositionM > 0, cancellationToken);
            var bottomTwoRows = await dbContext.Set<Member>().AsNoTracking()
                .CountAsync(x => x.PPositionM >= bottomTwoStart, cancellationToken);

            // Placement is strictly sequential (PoolTreePosition.NextPosition = max + 1), so the next
            // slot is arithmetic, not a search -- see PoolPlacementService.
            var nextPosition = maxPosition + 1;
            var parentPosition = PoolTreePosition.ParentOf(nextPosition);
            string? parentCode = null;
            if (parentPosition.HasValue)
            {
                parentCode = await dbContext.Set<Member>().AsNoTracking()
                    .Where(x => x.PPositionM == parentPosition.Value)
                    .Select(x => x.LoginID)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return new TreeHealthResponse
            {
                TreeDepth = treeDepth,
                LastRow = new TreeHealthLastRow
                {
                    Row = treeDepth,
                    Capacity = lastRowCapacity,
                    Filled = filledInLastRow,
                    FillPercent = lastRowCapacity > 0 ? Math.Round(filledInLastRow * 100m / lastRowCapacity, 1) : 0m
                },
                NextPlacement = new TreeHealthNextPlacement
                {
                    ParentCode = parentCode,
                    Side = PoolTreePosition.SideOf(nextPosition) == PoolSide.Left ? "LEFT" : "RIGHT",
                    Row = PoolTreePosition.DepthOf(nextPosition)
                },
                MembersInBottomTwoRows = bottomTwoRows,
                BottomTwoRowsPercent = totalMembers > 0 ? Math.Round(bottomTwoRows * 100m / totalMembers, 1) : 0m
            };
        }

        public async Task<StuckMembersResponse> GetStuckMembersAsync(int minDays, int limit, CancellationToken cancellationToken)
        {
            var safeLimit = Math.Clamp(limit, 1, 200);

            var members = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM != null && x.PPositionM > 0)
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.Mobile, x.RegDate })
                .ToListAsync(cancellationToken);

            var memberLevels = await dbContext.Set<MemberLevel>().AsNoTracking()
                .ToListAsync(cancellationToken);
            var levelsByRegNo = memberLevels.GroupBy(x => x.RegNo)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Levels));

            // When each completed level finished, so "days at the current level" can be measured from
            // the previous level's completion rather than needing a per-member lookup for each row.
            var completionDates = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Upgrade && x.Debit > 0 && x.MemberRegNo != null)
                .GroupBy(x => new { x.MemberRegNo, x.LevelNo })
                .Select(g => new { g.Key.MemberRegNo, g.Key.LevelNo, OccurredOn = g.Min(x => x.OccurredOn) })
                .ToListAsync(cancellationToken);
            var completionByRegNo = completionDates.GroupBy(x => x.MemberRegNo!.Value)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.LevelNo, x => x.OccurredOn));

            var earnedByRegNo = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.WalletType == PoolWalletType.Main && x.MemberRegNo != null)
                .GroupBy(x => x.MemberRegNo!.Value)
                .Select(g => new { RegNo = g.Key, Total = g.Sum(x => x.Credit) })
                .ToDictionaryAsync(x => x.RegNo, x => x.Total, cancellationToken);

            var levelMasters = await dbContext.Set<PoolLevelMaster>().AsNoTracking()
                .ToDictionaryAsync(x => x.LevelNo, cancellationToken);

            var rows = new List<StuckMemberRow>();
            var now = DateTime.Now;

            foreach (var m in members)
            {
                var regNo = m.RegNo ?? 0;
                var levels = levelsByRegNo.GetValueOrDefault(regNo);
                var completedCount = levels?.Values.Count(x => x.MlActive == true) ?? 0;
                var currentLevel = Math.Min(completedCount, 7);

                if (currentLevel >= 7)
                    continue; // final level has nothing left to be "stuck" waiting on

                var currentRow = levels?.GetValueOrDefault(currentLevel);
                var qualified = currentRow?.TlCount ?? 0;
                var required = levelMasters.GetValueOrDefault(currentLevel)?.RequiredMembers ?? 0;

                if (qualified >= required)
                    continue; // this level already has what it needs; the completion just hasn't run yet

                var priorCompletion = currentLevel == 0
                    ? m.RegDate
                    : completionByRegNo.GetValueOrDefault(regNo)?.GetValueOrDefault(currentLevel - 1) ?? m.RegDate;

                var daysAtLevel = priorCompletion.HasValue
                    ? (int)Math.Floor((now - priorCompletion.Value).TotalDays)
                    : 0;

                if (daysAtLevel < minDays)
                    continue;

                var totalEarned = earnedByRegNo.GetValueOrDefault(regNo);
                var riskBand = daysAtLevel >= RiskDays && totalEarned == 0
                    ? "RISK"
                    : daysAtLevel >= WatchDays
                        ? "WATCH"
                        : "NORMAL";

                rows.Add(new StuckMemberRow
                {
                    MemberRegNo = regNo,
                    RegistrationNumber = m.LoginID,
                    Name = Name(m.FName, m.LName),
                    CurrentLevel = currentLevel,
                    QualifiedMembers = qualified,
                    RequiredMembers = required,
                    DaysAtLevel = daysAtLevel,
                    TotalEarned = totalEarned,
                    RiskBand = riskBand,
                    Mobile = m.Mobile
                });
            }

            var ordered = rows.OrderByDescending(x => x.DaysAtLevel).ToList();

            return new StuckMembersResponse
            {
                Items = ordered.Take(safeLimit).ToList(),
                TotalStuck = ordered.Count
            };
        }

        private static string Name(string? fName, string? lName) => $"{fName} {lName}".Trim();
    }
}
