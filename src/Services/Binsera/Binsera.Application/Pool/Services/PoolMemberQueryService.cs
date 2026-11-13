using System.Linq.Expressions;
using Binsera.Core.Members.Domain;
using Binsera.Core.Pool;
using Binsera.Core.Pool.Contracts;
using Binsera.Core.Pool.Domain;
using Binsera.Core.Pool.Models;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Pool.Services
{
    /// <summary>
    /// Backs Tasks 1-6 of docs/plan/Member_Pool_API_And_UI_Plan.md -- everything a member reads
    /// about their own pool standing. Every method is scoped to the caller's own RegNo, and the
    /// tree/team lookups additionally check the requested root is the caller or their downline
    /// before returning anything (PART 16 of the UI design doc) -- enforced here, in the service,
    /// not left to the controller.
    /// </summary>
    [AutoRegister(includeInterfaces: true)]
    public class PoolMemberQueryService(BinseraDbContext dbContext) : IPoolMemberQueryService
    {
        /// <summary>
        /// UplinePath only carries each member's nearest 8 ancestors (the deepest a single
        /// level-completion transfer can reach) -- it is not a full chain to the root, so it
        /// cannot answer "how many members sit under me" for anyone more than 8 levels deep.
        /// Descendant listing/counting instead walks position ranges: a member at position P's
        /// descendants at relative depth d occupy positions [P*2^d, P*2^d + 2^d - 1], the same
        /// arithmetic placement itself uses. Bounded at 20 relative levels, which already covers
        /// over a million members under a single root -- deeper than any real downline reaches
        /// today.
        /// </summary>
        private const int MaxTeamDepth = 20;

        public async Task<MemberSummaryResponse> GetSummaryAsync(long regNo, CancellationToken cancellationToken)
        {
            var member = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.RegNo == regNo)
                .Select(x => new { x.FName, x.LName, x.LoginID, x.RegDate, x.ConfPayNo, Position = x.PPositionM })
                .FirstOrDefaultAsync(cancellationToken);

            if (member is null || member.Position is null || member.Position <= 0)
            {
                return new MemberSummaryResponse
                {
                    Member = new MemberInfo { RegNo = regNo, Name = Name(member?.FName, member?.LName) }
                };
            }

            var position = member.Position.Value;

            var completedLevels = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => x.RegNo == regNo && x.MlActive == true)
                .CountAsync(cancellationToken);
            var currentLevel = Math.Min(completedLevels, 7);

            var levelRow = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => x.RegNo == regNo && x.Levels == currentLevel)
                .Select(x => (int?)x.TlCount)
                .FirstOrDefaultAsync(cancellationToken);

            var levelMasters = await dbContext.Set<PoolLevelMaster>().AsNoTracking()
                .Where(x => x.LevelNo >= 1 && x.LevelNo <= 7)
                .OrderBy(x => x.LevelNo)
                .ToListAsync(cancellationToken);
            var currentLevelMaster = levelMasters.FirstOrDefault(x => x.LevelNo == currentLevel);

            var mainWallet = await dbContext.Set<MemberAccount>().AsNoTracking()
                .Where(x => x.RegNo == regNo && x.TrStatus == 1)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit) - g.Sum(x => x.Debit))
                .FirstOrDefaultAsync(cancellationToken);

            var upgradeWallet = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo == regNo && x.WalletType == PoolWalletType.Upgrade)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit) - g.Sum(x => x.Debit))
                .FirstOrDefaultAsync(cancellationToken);

            var thisWeek = await WeeklyMovementAsync(regNo, position, cancellationToken);

            var leftOccupied = await MemberExistsAtPositionAsync(PoolTreePosition.LeftChildOf(position), cancellationToken);
            var rightOccupied = await MemberExistsAtPositionAsync(PoolTreePosition.RightChildOf(position), cancellationToken);
            var (leftLegCount, rightLegCount, totalTeamCount) = await LegCountsAsync(position, cancellationToken);
            var completions = await CompletionDatesAsync(regNo, cancellationToken);

            var ladder = levelMasters.Select(m => new LadderRung
            {
                Level = m.LevelNo,
                State = m.LevelNo < currentLevel ? "DONE" : m.LevelNo == currentLevel ? "CURRENT" : "LOCKED",
                IncomeEarned = m.LevelNo < currentLevel ? m.MainWalletIncome : null,
                IncomeOnComplete = m.LevelNo >= currentLevel ? m.MainWalletIncome : null,
                CompletedOn = completions.GetValueOrDefault(m.LevelNo)
            }).ToList();

            var levelBreakup = levelMasters
                .Where(m => m.LevelNo < currentLevel)
                .Select(m => new LevelBreakupRow { Level = m.LevelNo, Amount = m.MainWalletIncome })
                .ToList();

            var requiredAmount = (currentLevelMaster?.RequiredMembers ?? 0) * (currentLevelMaster?.ContributionPerMember ?? 0);
            var collectedAmount = (levelRow ?? 0) * (currentLevelMaster?.ContributionPerMember ?? 0);

            LevelPoolSummary? levelPool = null;
            if (currentLevel <= 7 && currentLevelMaster is not null)
            {
                var contributions = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                    .Where(x => x.MemberRegNo == regNo && x.WalletType == PoolWalletType.Upgrade
                                && x.LevelNo == currentLevel && x.Credit > 0)
                    .OrderByDescending(x => x.Id)
                    .Take(10)
                    .Select(x => new { x.SourceRegNo, x.Credit, x.OccurredOn })
                    .ToListAsync(cancellationToken);

                var sourceRegNos = contributions.Where(x => x.SourceRegNo.HasValue).Select(x => x.SourceRegNo!.Value).ToList();
                var codes = await dbContext.Set<Member>().AsNoTracking()
                    .Where(x => x.RegNo != null && sourceRegNos.Contains(x.RegNo.Value))
                    .ToDictionaryAsync(x => x.RegNo!.Value, x => x.LoginID, cancellationToken);

                levelPool = new LevelPoolSummary
                {
                    Level = currentLevel,
                    Balance = upgradeWallet,
                    Withdrawable = false,
                    Contributions = contributions.Select(c => new PoolContribution
                    {
                        MemberCode = c.SourceRegNo.HasValue ? codes.GetValueOrDefault(c.SourceRegNo.Value, "") : "",
                        Amount = c.Credit,
                        ReceivedOn = c.OccurredOn
                    }).ToList()
                };
            }

            return new MemberSummaryResponse
            {
                Member = new MemberInfo
                {
                    RegNo = regNo,
                    RegistrationNumber = member.LoginID,
                    Name = Name(member.FName, member.LName),
                    JoinedOn = member.RegDate,
                    Status = member.ConfPayNo == 1 ? "ACTIVE" : "PENDING"
                },
                CurrentLevel = currentLevel,
                Ladder = ladder,
                CurrentLevelProgress = new LevelProgress
                {
                    Level = currentLevel,
                    CollectedAmount = collectedAmount,
                    RequiredAmount = requiredAmount,
                    QualifiedMembers = levelRow ?? 0,
                    RequiredMembers = currentLevelMaster?.RequiredMembers ?? 0,
                    FillPercent = requiredAmount > 0 ? Math.Round(collectedAmount / requiredAmount * 100, 1) : 0,
                    IncomeOnComplete = currentLevelMaster?.MainWalletIncome ?? 0,
                    NextLevel = currentLevel < 7 ? currentLevel + 1 : null
                },
                MainWallet = new MainWalletSummary
                {
                    Balance = mainWallet,
                    CanWithdraw = true,
                    LevelBreakup = levelBreakup
                },
                LevelPool = levelPool,
                Team = new TeamStats
                {
                    Direct = (leftOccupied ? 1 : 0) + (rightOccupied ? 1 : 0),
                    Total = totalTeamCount,
                    Left = leftLegCount,
                    Right = rightLegCount
                },
                ThisWeek = thisWeek
            };
        }

        /// <summary>
        /// The last seven days below this member: who arrived, and what the pool paid. Both filtered
        /// in SQL -- a downline is not something to pull into memory just to count a week.
        /// </summary>
        private async Task<WeeklyMovement> WeeklyMovementAsync(
            long regNo, long position, CancellationToken cancellationToken)
        {
            // DateTime.Now, not UtcNow: RegDate and OccurredOn are both written in server-local time.
            var since = DateTime.Now.AddDays(-7);

            var newMembers = await dbContext.Set<Member>().AsNoTracking()
                .Where(DescendantPredicate(position))
                .CountAsync(x => x.RegDate >= since, cancellationToken);

            var earned = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo == regNo
                            && x.WalletType == PoolWalletType.Main
                            && x.OccurredOn >= since)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit))
                .FirstOrDefaultAsync(cancellationToken);

            return new WeeklyMovement { NewMembers = newMembers, Earned = earned };
        }

        public async Task<List<ActivityItem>> GetActivityAsync(long regNo, int limit, CancellationToken cancellationToken)
        {
            var completedLevels = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => x.RegNo == regNo && x.MlActive == true)
                .CountAsync(cancellationToken);
            var currentLevel = Math.Min(completedLevels, 7);

            var completions = await CompletionDatesAsync(regNo, cancellationToken);
            var levelMasters = await dbContext.Set<PoolLevelMaster>().AsNoTracking()
                .ToDictionaryAsync(x => x.LevelNo, cancellationToken);

            // Level 0 is the joining fee, an internal modelling device -- the member-facing ladder
            // starts at Level 1 and there is no "Level 0" anywhere in the UI, so surfacing
            // "Level 0 completed" in the feed would name something the member has never seen.
            var levelCompleteItems = completions
                .Where(kv => kv.Key >= 1 && kv.Value.HasValue)
                .Select(kv => new ActivityItem
                {
                    Type = "LEVEL_COMPLETE",
                    Title = $"Level {kv.Key} completed",
                    Date = kv.Value!.Value,
                    Amount = levelMasters.GetValueOrDefault(kv.Key)?.MainWalletIncome ?? 0,
                    Level = kv.Key,
                    Display = levelMasters.GetValueOrDefault(kv.Key)?.MainWalletIncome > 0
                        ? $"+₹{levelMasters[kv.Key].MainWalletIncome:N0}"
                        : "₹0",
                    AffectsMainWallet = true
                });

            var contributions = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo == regNo && x.WalletType == PoolWalletType.Upgrade
                            && x.LevelNo == currentLevel && x.Credit > 0)
                .OrderByDescending(x => x.Id)
                .Take(limit)
                .Select(x => new { x.SourceRegNo, x.Credit, x.OccurredOn, x.LevelNo })
                .ToListAsync(cancellationToken);

            var sourceRegNos = contributions.Where(x => x.SourceRegNo.HasValue).Select(x => x.SourceRegNo!.Value).ToList();
            var codes = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.RegNo != null && sourceRegNos.Contains(x.RegNo.Value))
                .ToDictionaryAsync(x => x.RegNo!.Value, x => x.LoginID, cancellationToken);

            var poolCreditItems = contributions.Select(c =>
            {
                var code = c.SourceRegNo.HasValue ? codes.GetValueOrDefault(c.SourceRegNo.Value, "") : "";
                return new ActivityItem
                {
                    Type = "POOL_CREDIT",
                    Title = $"{code} joined your team",
                    Date = c.OccurredOn,
                    Amount = c.Credit,
                    Level = c.LevelNo,
                    Display = $"+₹{c.Credit:N0} to Level {c.LevelNo}",
                    AffectsMainWallet = false
                };
            });

            return levelCompleteItems.Concat(poolCreditItems)
                .OrderByDescending(x => x.Date)
                .Take(limit)
                .ToList();
        }

        public async Task<List<TreeNodeResponse>> GetTreeAsync(
            long regNo, long rootRegNo, int depth, CancellationToken cancellationToken)
        {
            depth = Math.Clamp(depth, 1, 5);

            await GuardSelfOrDownlineAsync(regNo, rootRegNo, cancellationToken);

            var root = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.RegNo == rootRegNo)
                .Select(x => new { x.PPositionM })
                .FirstOrDefaultAsync(cancellationToken);

            if (root?.PPositionM is null || root.PPositionM <= 0)
                return new List<TreeNodeResponse>();

            var rootPosition = root.PPositionM.Value;
            var rootDepth = PoolTreePosition.DepthOf(rootPosition);

            // Fetch one extra level so hasChildren is known for the deepest returned row.
            var low = rootPosition;
            var high = rootPosition * (1L << (depth + 1)) + (1L << (depth + 1)) - 1;

            var rows = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM >= low && x.PPositionM <= high)
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.PPositionM })
                .ToListAsync(cancellationToken);

            var byPosition = rows.Where(x => x.PPositionM.HasValue).ToDictionary(x => x.PPositionM!.Value);
            var levels = await LevelsByRegNoAsync(rows.Select(x => x.RegNo ?? 0).ToList(), cancellationToken);

            var result = new List<TreeNodeResponse>();
            foreach (var row in rows)
            {
                var position = row.PPositionM!.Value;
                var relativeDepth = PoolTreePosition.DepthOf(position) - rootDepth;
                if (relativeDepth < 0 || relativeDepth > depth)
                    continue;

                // Same ancestry check as BuildBinaryNodesAsync -- without it a sibling's subtree,
                // which shares both the depth range and the position span, would come back as part
                // of this member's tree.
                if ((position >> relativeDepth) != rootPosition)
                    continue;

                var hasChildren = byPosition.ContainsKey(PoolTreePosition.LeftChildOf(position))
                                   || byPosition.ContainsKey(PoolTreePosition.RightChildOf(position));

                result.Add(new TreeNodeResponse
                {
                    RegNo = row.RegNo ?? 0,
                    RegistrationNumber = row.LoginID,
                    Name = Name(row.FName, row.LName),
                    Position = position,
                    ParentPosition = position == rootPosition ? null : PoolTreePosition.ParentOf(position),
                    CurrentLevel = levels.GetValueOrDefault(row.RegNo ?? 0),
                    Depth = relativeDepth,
                    HasChildren = hasChildren
                });
            }

            return result;
        }

        public async Task<BinaryTreeResponse> GetBinaryTreeAsync(
            long callerRegNo, bool isAdmin, long rootRegNo, int depth, CancellationToken cancellationToken)
        {
            depth = Math.Clamp(depth, 1, 5);

            // The admin tree pages open on member id 1, which in the pool is the X1 seed account --
            // it sits above the tree and holds no position, so it would render nothing. For an
            // admin, fall back to the first real member instead of showing an empty tree.
            if (isAdmin && await GetPositionAsync(rootRegNo, cancellationToken) is null)
                rootRegNo = await PoolRootRegNoAsync(cancellationToken) ?? rootRegNo;

            if (!isAdmin)
                await GuardSelfOrDownlineAsync(callerRegNo, rootRegNo, cancellationToken);

            var nodes = await BuildBinaryNodesAsync(rootRegNo, depth, cancellationToken);
            if (nodes.Count == 0)
                return new BinaryTreeResponse { Meta = new BinaryTreeMeta { RootId = rootRegNo, Depth = depth } };

            var root = nodes[0];
            var occupied = nodes.Count(n => !n.IsEmpty);
            var rootPosition = await GetPositionAsync(rootRegNo, cancellationToken) ?? 0;
            var totalBelow = rootPosition > 0 ? await CountDescendantsAsync(rootPosition, cancellationToken) : 0;

            return new BinaryTreeResponse
            {
                Root = root,
                Members = nodes,
                Meta = new BinaryTreeMeta
                {
                    RootId = rootRegNo,
                    Depth = depth,
                    // Vacancies are drawn but they are not members, so they stay out of the counts.
                    TotalMembers = occupied,
                    Returned = occupied,
                    MaxDepth = depth,
                    RootTotal = totalBelow
                }
            };
        }

        public async Task<BinarySubtreeResponse> GetBinarySubtreeAsync(
            long callerRegNo, bool isAdmin, long parentRegNo, int depth, CancellationToken cancellationToken)
        {
            depth = Math.Clamp(depth, 1, 5);

            if (!isAdmin)
                await GuardSelfOrDownlineAsync(callerRegNo, parentRegNo, cancellationToken);

            var nodes = await BuildBinaryNodesAsync(parentRegNo, depth, cancellationToken);

            // The caller already has the parent rendered; only what hangs below it is new.
            var below = nodes.Where(n => n.Id != parentRegNo.ToString()).ToList();

            return new BinarySubtreeResponse
            {
                ParentId = parentRegNo,
                Members = below,
                Depth = depth,
                Returned = below.Count
            };
        }

        public async Task<BinaryMemberDetailResponse?> GetBinaryMemberAsync(
            long callerRegNo, bool isAdmin, long regNo, CancellationToken cancellationToken)
        {
            if (!isAdmin)
                await GuardSelfOrDownlineAsync(callerRegNo, regNo, cancellationToken);

            // Depth 0 -- just this member, with hasChildren/left/right filled in as usual.
            var nodes = await BuildBinaryNodesAsync(regNo, 0, cancellationToken);
            var node = nodes.FirstOrDefault();
            if (node is null)
                return null;

            var upline = await GetBinaryUplineAsync(callerRegNo, isAdmin, regNo, cancellationToken);

            return new BinaryMemberDetailResponse
            {
                Id = node.Id,
                RegistrationNumber = node.RegistrationNumber,
                Name = node.Name,
                Role = node.Role,
                Package = node.Package,
                Image = node.Image,
                Status = node.Status,
                ParentId = node.ParentId,
                Placement = node.Placement,
                Level = node.Level,
                Earnings = node.Earnings,
                JoinedOn = node.JoinedOn,
                IsEmpty = node.IsEmpty,
                HasChildren = node.HasChildren,
                LeftChildExists = node.LeftChildExists,
                RightChildExists = node.RightChildExists,
                ChildrenCount = node.ChildrenCount,
                Upline = upline.Path
            };
        }

        /// <summary>
        /// Path from the viewer's own root down to the requested member, for the breadcrumb.
        ///
        /// It deliberately stops at the caller: a member may not see who sits above them, so the
        /// walk climbs only as far as their own position and no further. An admin gets the path
        /// back to the pool root.
        /// </summary>
        public async Task<BinaryUplineResponse> GetBinaryUplineAsync(
            long callerRegNo, bool isAdmin, long regNo, CancellationToken cancellationToken)
        {
            if (!isAdmin)
                await GuardSelfOrDownlineAsync(callerRegNo, regNo, cancellationToken);

            var targetPosition = await GetPositionAsync(regNo, cancellationToken);
            if (targetPosition is null)
                return new BinaryUplineResponse();

            var stopPosition = isAdmin
                ? PoolTreePosition.RootPosition
                : await GetPositionAsync(callerRegNo, cancellationToken) ?? PoolTreePosition.RootPosition;

            var positions = new List<long>();
            var current = targetPosition.Value;
            while (true)
            {
                positions.Add(current);
                if (current == stopPosition)
                    break;

                var parent = PoolTreePosition.ParentOf(current);
                if (parent is null)
                    break;

                current = parent.Value;
            }

            positions.Reverse();

            var rows = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM != null && positions.Contains(x.PPositionM.Value))
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.PPositionM })
                .ToListAsync(cancellationToken);

            var byPosition = rows.ToDictionary(x => x.PPositionM!.Value);

            return new BinaryUplineResponse
            {
                Path = positions
                    .Where(byPosition.ContainsKey)
                    .Select(p => new BinaryUplineEntry
                    {
                        Id = byPosition[p].RegNo ?? 0,
                        Name = Name(byPosition[p].FName, byPosition[p].LName),
                        RegistrationNumber = byPosition[p].LoginID
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Member-code or name search, restricted to the caller's own downline -- the search box
        /// must not become a way to look up members the tree itself would refuse to show.
        /// </summary>
        public async Task<BinarySearchResponse> SearchBinaryMembersAsync(
            long callerRegNo, bool isAdmin, string query, int limit, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new BinarySearchResponse();

            limit = Math.Clamp(limit, 1, 50);
            var term = query.Trim();

            var candidates = dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM > 0
                            && (x.LoginID.Contains(term) || (x.FName + " " + x.LName).Contains(term)));

            if (!isAdmin)
            {
                var callerPosition = await GetPositionAsync(callerRegNo, cancellationToken);
                if (callerPosition is null)
                    return new BinarySearchResponse();

                candidates = candidates.Where(DescendantPredicate(callerPosition.Value));
            }

            var rows = await candidates
                .OrderBy(x => x.PPositionM)
                .Take(limit)
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.ConfPayNo, x.PPositionM })
                .ToListAsync(cancellationToken);

            var levels = await LevelsByRegNoAsync(rows.Select(x => x.RegNo ?? 0).ToList(), cancellationToken);

            var members = rows.Select(row =>
            {
                var regNo = row.RegNo ?? 0;
                return new BinaryTreeNode
                {
                    Id = regNo.ToString(),
                    RegistrationNumber = row.LoginID,
                    Name = Name(row.FName, row.LName),
                    Role = "Member",
                    Package = $"Level {levels.GetValueOrDefault(regNo)}",
                    Status = row.ConfPayNo == 1 ? "active" : "pending",
                    Level = levels.GetValueOrDefault(regNo),
                    Placement = row.PPositionM == PoolTreePosition.RootPosition
                        ? null
                        : PoolTreePosition.SideOf(row.PPositionM!.Value) == PoolSide.Left ? "left" : "right"
                };
            }).ToList();

            return new BinarySearchResponse
            {
                Members = members,
                Meta = new BinaryTreeMeta { TotalMembers = members.Count, Returned = members.Count }
            };
        }

        private async Task<List<BinaryTreeNode>> BuildBinaryNodesAsync(
            long rootRegNo, int depth, CancellationToken cancellationToken)
        {
            var root = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.RegNo == rootRegNo)
                .Select(x => new { x.PPositionM })
                .FirstOrDefaultAsync(cancellationToken);

            if (root?.PPositionM is null || root.PPositionM <= 0)
                return new List<BinaryTreeNode>();

            var rootPosition = root.PPositionM.Value;
            var rootDepth = PoolTreePosition.DepthOf(rootPosition);

            // One level deeper than asked for, so hasChildren is known for the deepest row returned.
            var high = rootPosition * (1L << (depth + 1)) + (1L << (depth + 1)) - 1;

            var rows = await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM >= rootPosition && x.PPositionM <= high)
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.ConfPayNo, x.PPositionM, x.RegDate, x.ProfileImage })
                .ToListAsync(cancellationToken);

            var byPosition = rows.Where(x => x.PPositionM.HasValue).ToDictionary(x => x.PPositionM!.Value);
            var regNos = rows.Select(x => x.RegNo ?? 0).ToList();
            var levels = await LevelsByRegNoAsync(regNos, cancellationToken);
            var earnings = await EarningsByRegNoAsync(regNos, cancellationToken);

            var nodes = new List<BinaryTreeNode>();
            foreach (var row in rows.OrderBy(x => x.PPositionM))
            {
                var position = row.PPositionM!.Value;
                var relativeDepth = PoolTreePosition.DepthOf(position) - rootDepth;
                if (relativeDepth < 0 || relativeDepth > depth)
                    continue;

                // Depth alone does not prove ancestry -- a sibling and its whole subtree sit at the
                // same depths and inside the same position span. Walking the position back up by
                // the depth difference has to land exactly on the root, or this node belongs to
                // somebody else's branch and must not be returned.
                if ((position >> relativeDepth) != rootPosition)
                    continue;

                var leftExists = byPosition.ContainsKey(PoolTreePosition.LeftChildOf(position));
                var rightExists = byPosition.ContainsKey(PoolTreePosition.RightChildOf(position));
                var regNo = row.RegNo ?? 0;

                nodes.Add(new BinaryTreeNode
                {
                    Id = regNo.ToString(),
                    RegistrationNumber = row.LoginID,
                    Name = Name(row.FName, row.LName),
                    Role = "Member",
                    Package = $"Level {levels.GetValueOrDefault(regNo)}",
                    // The media id, not a URL -- the client turns it into one, the same way every
                    // other screen does. Blank where the member never uploaded a photo, which is
                    // the signal to fall back to their initials.
                    Image = row.ProfileImage,
                    Status = row.ConfPayNo == 1 ? "active" : "pending",
                    ParentId = position == rootPosition
                        ? null
                        : byPosition.TryGetValue(PoolTreePosition.ParentOf(position) ?? 0, out var parent)
                            ? (parent.RegNo ?? 0).ToString()
                            : null,
                    Placement = position == rootPosition
                        ? null
                        : PoolTreePosition.SideOf(position) == PoolSide.Left ? "left" : "right",
                    Level = levels.GetValueOrDefault(regNo),
                    Earnings = earnings.GetValueOrDefault(regNo),
                    JoinedOn = row.RegDate,
                    IsEmpty = false,
                    HasChildren = leftExists || rightExists,
                    LeftChildExists = leftExists,
                    RightChildExists = rightExists,
                    ChildrenCount = (leftExists ? 1 : 0) + (rightExists ? 1 : 0)
                });

                // A vacant position is information, not absence: it is where the next member lands.
                // Without a node for it the tree draws a single child and the viewer cannot tell a
                // filled leg from an open one. Only emitted above the requested depth so the
                // deepest returned row does not sprout placeholders below the visible tree.
                if (relativeDepth < depth)
                {
                    if (!leftExists)
                        nodes.Add(VacancyNode(PoolTreePosition.LeftChildOf(position), regNo, PoolSide.Left));
                    if (!rightExists)
                        nodes.Add(VacancyNode(PoolTreePosition.RightChildOf(position), regNo, PoolSide.Right));
                }
            }

            return nodes;
        }

        /// <summary>
        /// An open slot. The id is the negated position: unique, stable, and impossible to collide
        /// with a real RegNo, which is what the tree component keys its nodes on.
        /// </summary>
        private static BinaryTreeNode VacancyNode(long position, long parentRegNo, PoolSide side) => new()
        {
            Id = (-position).ToString(),
            Name = string.Empty,
            Status = string.Empty,
            ParentId = parentRegNo.ToString(),
            Placement = side == PoolSide.Left ? "left" : "right",
            IsEmpty = true,
            HasChildren = false
        };

        public async Task<TeamListResponse> GetTeamAsync(
            long regNo, string? search, int? level, int page, int pageSize, CancellationToken cancellationToken)
        {
            var position = await GetPositionAsync(regNo, cancellationToken);
            if (position is null)
                return new TeamListResponse();

            // Filtering and paging happen in SQL. This used to pull the entire downline into memory
            // before paging it, which is fine for a handful of members and steadily worse as a tree
            // grows -- the page the caller asked for is the only thing that should cross the wire.
            var descendants = dbContext.Set<Member>().AsNoTracking()
                .Where(DescendantPredicate(position.Value));

            var filtered = descendants;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(x => x.LoginID.Contains(term)
                                               || (x.FName + " " + x.LName).Contains(term));
            }

            // Level lives in Member_Level, so filtering on it needs a subquery rather than a join --
            // a member's level is how many of their rows are complete.
            if (level.HasValue)
            {
                var wanted = level.Value;
                filtered = filtered.Where(x => dbContext.Set<MemberLevel>()
                    .Count(l => l.RegNo == x.RegNo && l.MlActive == true) == wanted);
            }

            var totalItems = await filtered.CountAsync(cancellationToken);

            var pageRows = await filtered
                .OrderBy(x => x.PPositionM)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.PPositionM, x.RegDate, x.ProfileImage })
                .ToListAsync(cancellationToken);

            var levels = await LevelsByRegNoAsync(pageRows.Select(x => x.RegNo ?? 0).ToList(), cancellationToken);
            var leftChild = PoolTreePosition.LeftChildOf(position.Value);
            var rootDepth = PoolTreePosition.DepthOf(position.Value);
            var (leftLeg, rightLeg, total) = await LegCountsAsync(position.Value, cancellationToken);
            var directCount = await CountDirectAsync(position.Value, cancellationToken);

            return new TeamListResponse
            {
                DirectCount = directCount,
                TotalTeamCount = total,
                LeftLegCount = leftLeg,
                RightLegCount = rightLeg,
                TotalItems = totalItems,
                Items = pageRows.Select(x =>
                {
                    var pos = x.PPositionM!.Value;
                    var relativeDepth = PoolTreePosition.DepthOf(pos) - rootDepth;
                    return new TeamMemberRow
                    {
                        RegNo = x.RegNo ?? 0,
                        ProfileImage = x.ProfileImage,
                        RegistrationNumber = x.LoginID,
                        Name = Name(x.FName, x.LName),
                        CurrentLevel = levels.GetValueOrDefault(x.RegNo ?? 0),
                        Depth = relativeDepth,
                        JoinedOn = x.RegDate,
                        Side = (pos >> (relativeDepth - 1)) == leftChild ? "Left" : "Right"
                    };
                }).ToList()
            };
        }

        /// <summary>The first real member -- the one holding position 1, directly under X1.</summary>
        private async Task<long?> PoolRootRegNoAsync(CancellationToken cancellationToken)
            => await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.PPositionM == PoolTreePosition.RootPosition)
                .Select(x => x.RegNo)
                .FirstOrDefaultAsync(cancellationToken);

        /// <summary>Members hanging directly off this position -- at most two, by definition.</summary>
        private async Task<int> CountDirectAsync(long rootPosition, CancellationToken cancellationToken)
        {
            var left = PoolTreePosition.LeftChildOf(rootPosition);
            var right = PoolTreePosition.RightChildOf(rootPosition);

            return await dbContext.Set<Member>().AsNoTracking()
                .CountAsync(x => x.PPositionM == left || x.PPositionM == right, cancellationToken);
        }

        public async Task<IncomeStatementResponse> GetIncomeAsync(long regNo, CancellationToken cancellationToken)
        {
            var levelRows = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => x.RegNo == regNo)
                .ToDictionaryAsync(x => x.Levels, cancellationToken);

            // Level 0 is the joining fee and has no rung on the member-facing ladder, so it has no
            // line here either -- same reasoning as the activity feed.
            var masters = await dbContext.Set<PoolLevelMaster>().AsNoTracking()
                .Where(x => x.LevelNo >= 1)
                .OrderBy(x => x.LevelNo)
                .ToListAsync(cancellationToken);

            var completions = await CompletionDatesAsync(regNo, cancellationToken);
            var currentLevel = Math.Min(levelRows.Count(x => x.Value.MlActive == true), 7);

            var summary = masters.Select(m =>
            {
                var state = m.LevelNo < currentLevel ? "DONE"
                    : m.LevelNo == currentLevel ? "CURRENT"
                    : "LOCKED";

                var received = levelRows.TryGetValue(m.LevelNo, out var row) ? row.TlCount : 0;

                return new LevelIncomeRow
                {
                    LevelNo = m.LevelNo,
                    State = state,
                    Earned = state == "DONE" ? m.MainWalletIncome : null,
                    PayableOnComplete = state == "DONE" ? null : m.MainWalletIncome,
                    CompletedOn = completions.GetValueOrDefault(m.LevelNo),
                    ReceivedMembers = received,
                    RequiredMembers = m.RequiredMembers,
                    // A completed level has drained, so what it collected is its full requirement.
                    CollectedAmount = state == "DONE"
                        ? m.RequiredMembers * m.ContributionPerMember
                        : received * m.ContributionPerMember,
                    RequiredAmount = m.RequiredMembers * m.ContributionPerMember,
                    TransferAmount = m.TransferAmount,
                    StepsUp = m.StepsUp ?? 0
                };
            }).ToList();

            // Only the total is needed here -- the rows themselves belong to the wallet ledger.
            var totalEarned = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo == regNo && x.WalletType == PoolWalletType.Main)
                .GroupBy(x => 1)
                .Select(g => g.Sum(x => x.Credit))
                .FirstOrDefaultAsync(cancellationToken);

            return new IncomeStatementResponse
            {
                TotalEarned = totalEarned,
                PendingFromCurrentLevel = summary
                    .FirstOrDefault(x => x.State == "CURRENT")?.PayableOnComplete ?? 0,
                CurrentLevel = currentLevel,
                LevelSummary = summary
            };
        }

        public async Task<List<WalletLedgerEntry>> GetWalletLedgerAsync(
            long regNo, bool mainWallet, DateTime? from, DateTime? to, CancellationToken cancellationToken)
        {
            var walletType = mainWallet ? PoolWalletType.Main : PoolWalletType.Upgrade;

            var query = dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo == regNo && x.WalletType == walletType);

            if (from.HasValue)
                query = query.Where(x => x.OccurredOn >= from.Value);
            if (to.HasValue)
                query = query.Where(x => x.OccurredOn <= to.Value);

            var rows = await query
                .OrderBy(x => x.Id)
                .Select(x => new { x.OccurredOn, x.Remark, x.Credit, x.Debit, x.LevelNo })
                .ToListAsync(cancellationToken);

            var balance = 0m;
            var result = new List<WalletLedgerEntry>(rows.Count);
            foreach (var row in rows)
            {
                balance += row.Credit - row.Debit;
                result.Add(new WalletLedgerEntry
                {
                    OccurredOn = row.OccurredOn,
                    Description = row.Remark,
                    Credit = row.Credit,
                    Debit = row.Debit,
                    Balance = balance,
                    LevelNo = row.LevelNo
                });
            }

            result.Reverse(); // most recent first, matching the doc's mockup
            return result;
        }

        public async Task<List<UpgradeHistoryEntry>> GetUpgradeHistoryAsync(long regNo, CancellationToken cancellationToken)
        {
            var levelRows = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => x.RegNo == regNo && x.MlActive == true)
                .Select(x => x.Levels)
                .ToListAsync(cancellationToken);

            if (levelRows.Count == 0)
                return new List<UpgradeHistoryEntry>();

            var masters = await dbContext.Set<PoolLevelMaster>().AsNoTracking()
                .Where(x => levelRows.Contains(x.LevelNo))
                .ToDictionaryAsync(x => x.LevelNo, cancellationToken);

            var completions = await CompletionDatesAsync(regNo, cancellationToken);

            return levelRows.OrderBy(n => n).Select(n =>
            {
                var m = masters[n];
                return new UpgradeHistoryEntry
                {
                    LevelNo = n,
                    NextLevelNo = n + 1,
                    AmountTransferred = m.TransferAmount,
                    StepsUp = m.StepsUp ?? 0,
                    MainWalletIncome = m.MainWalletIncome,
                    CompletedOn = completions.GetValueOrDefault(n)
                };
            }).ToList();
        }

        // ---- helpers -------------------------------------------------------------------------

        private static string Name(string? fName, string? lName) => $"{fName} {lName}".Trim();

        private async Task<long?> GetPositionAsync(long regNo, CancellationToken cancellationToken)
            => await dbContext.Set<Member>().AsNoTracking()
                .Where(x => x.RegNo == regNo && x.PPositionM > 0)
                .Select(x => x.PPositionM)
                .FirstOrDefaultAsync(cancellationToken);

        private async Task<bool> MemberExistsAtPositionAsync(long position, CancellationToken cancellationToken)
            => await dbContext.Set<Member>().AsNoTracking().AnyAsync(x => x.PPositionM == position, cancellationToken);

        private static List<(long Low, long High)> DescendantRanges(long rootPosition)
            => Enumerable.Range(1, MaxTeamDepth)
                .Select(d =>
                {
                    var span = 1L << d;
                    var low = rootPosition * span;
                    return (Low: low, High: low + span - 1);
                })
                .ToList();

        /// <summary>
        /// "Is this member's position inside any of the descendant ranges" as an OR chain built by
        /// hand, since EF Core cannot translate a captured List&lt;T&gt;.Any(...) lambda into SQL --
        /// the range bounds have to be baked into the expression tree itself as constants, the same
        /// way a hand-rolled PredicateBuilder would.
        /// </summary>
        private static Expression<Func<Member, bool>> DescendantPredicate(long rootPosition)
        {
            var parameter = Expression.Parameter(typeof(Member), "x");
            var positionProperty = Expression.Property(parameter, nameof(Member.PPositionM));

            Expression? body = null;
            foreach (var (low, high) in DescendantRanges(rootPosition))
            {
                var inRange = Expression.AndAlso(
                    Expression.GreaterThanOrEqual(positionProperty, Expression.Constant((long?)low, typeof(long?))),
                    Expression.LessThanOrEqual(positionProperty, Expression.Constant((long?)high, typeof(long?))));

                body = body is null ? inRange : Expression.OrElse(body, inRange);
            }

            return Expression.Lambda<Func<Member, bool>>(body!, parameter);
        }

        private async Task<int> CountDescendantsAsync(long rootPosition, CancellationToken cancellationToken)
            => await dbContext.Set<Member>().AsNoTracking()
                .Where(DescendantPredicate(rootPosition))
                .CountAsync(cancellationToken);

        /// <summary>
        /// Team.left/Team.right in the dashboard spec are full leg totals (everyone under the left
        /// child vs the right child), not just whether the immediate left/right slot is occupied --
        /// the spec's own example has them summing to the total (6 + 8 = 14). A descendant's leg is
        /// found by walking its position back up to the depth just below root and checking which
        /// child that lands on.
        /// </summary>
        private async Task<(int Left, int Right, int Total)> LegCountsAsync(long rootPosition, CancellationToken cancellationToken)
        {
            // Counted in SQL rather than by materialising every descendant position: each leg is
            // itself a subtree, so counting the descendants of the left child and of the right
            // child gives both legs, and their sum is the whole team.
            var leftChild = PoolTreePosition.LeftChildOf(rootPosition);
            var rightChild = PoolTreePosition.RightChildOf(rootPosition);

            var leftSubtree = await CountDescendantsAsync(leftChild, cancellationToken);
            var rightSubtree = await CountDescendantsAsync(rightChild, cancellationToken);

            // The two children themselves are part of their own legs.
            var leftOccupied = await MemberExistsAtPositionAsync(leftChild, cancellationToken) ? 1 : 0;
            var rightOccupied = await MemberExistsAtPositionAsync(rightChild, cancellationToken) ? 1 : 0;

            var left = leftSubtree + leftOccupied;
            var right = rightSubtree + rightOccupied;

            return (left, right, left + right);
        }

        /// <summary>
        /// True if <paramref name="targetRegNo"/> is <paramref name="callerRegNo"/> themself or sits
        /// in their downline. Position-based, same reasoning as <see cref="DescendantRanges"/> --
        /// walking up from the target by its own depth-relative-to-caller steps must land back on
        /// the caller.
        /// </summary>
        private async Task GuardSelfOrDownlineAsync(long callerRegNo, long targetRegNo, CancellationToken cancellationToken)
        {
            if (callerRegNo == targetRegNo)
                return;

            var callerPosition = await GetPositionAsync(callerRegNo, cancellationToken);
            var targetPosition = await GetPositionAsync(targetRegNo, cancellationToken);

            if (callerPosition is null || targetPosition is null)
                throw new UnauthorizedAccessException("Member is not placed in the pool.");

            var relativeDepth = PoolTreePosition.DepthOf(targetPosition.Value) - PoolTreePosition.DepthOf(callerPosition.Value);
            if (relativeDepth <= 0 || (targetPosition.Value >> relativeDepth) != callerPosition.Value)
                throw new UnauthorizedAccessException("Requested member is not in the caller's downline.");
        }

        private async Task<Dictionary<long, int>> LevelsByRegNoAsync(List<long> regNos, CancellationToken cancellationToken)
        {
            if (regNos.Count == 0)
                return new Dictionary<long, int>();

            var rows = await dbContext.Set<MemberLevel>().AsNoTracking()
                .Where(x => regNos.Contains(x.RegNo) && x.MlActive == true)
                .GroupBy(x => x.RegNo)
                .Select(g => new { RegNo = g.Key, Completed = g.Count() })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(x => x.RegNo, x => Math.Min(x.Completed, 7));
        }

        /// <summary>
        /// Lifetime Main-wallet credits per member, the same figure the Income screen totals. Batched
        /// because the tree asks about every node it draws at once.
        /// </summary>
        private async Task<Dictionary<long, decimal>> EarningsByRegNoAsync(
            List<long> regNos, CancellationToken cancellationToken)
        {
            if (regNos.Count == 0)
                return new Dictionary<long, decimal>();

            var rows = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo != null
                            && regNos.Contains(x.MemberRegNo.Value)
                            && x.WalletType == PoolWalletType.Main)
                .GroupBy(x => x.MemberRegNo!.Value)
                .Select(g => new { RegNo = g.Key, Total = g.Sum(x => x.Credit) })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(x => x.RegNo, x => x.Total);
        }

        /// <summary>
        /// When each level completed, for the "completed on" columns. Level 0 has no upward-transfer
        /// row of its own to date from (its fee moves in the same instant it's created), so it falls
        /// back to the member's own placement time; every other level dates from its LVLUP debit row,
        /// which exists for every completed level (level 7 has no further transfer but still drains
        /// its own wallet, so the row exists there too).
        /// </summary>
        private async Task<Dictionary<int, DateTime?>> CompletionDatesAsync(long regNo, CancellationToken cancellationToken)
        {
            var dates = await dbContext.Set<PoolWalletTransaction>().AsNoTracking()
                .Where(x => x.MemberRegNo == regNo && x.WalletType == PoolWalletType.Upgrade && x.Debit > 0)
                .GroupBy(x => x.LevelNo)
                .Select(g => new { LevelNo = g.Key, OccurredOn = g.Min(x => x.OccurredOn) })
                .ToDictionaryAsync(x => x.LevelNo, x => (DateTime?)x.OccurredOn, cancellationToken);

            if (!dates.ContainsKey(0))
            {
                var joinedOn = await dbContext.Set<Member>().AsNoTracking()
                    .Where(x => x.RegNo == regNo)
                    .Select(x => x.RegDate)
                    .FirstOrDefaultAsync(cancellationToken);
                dates[0] = joinedOn;
            }

            return dates;
        }
    }
}
