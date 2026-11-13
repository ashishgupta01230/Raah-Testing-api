using Binsera.Core.Members.Domain;
using Binsera.Core.Pool;
using Binsera.Core.Pool.Contracts;
using Binsera.Core.Pool.Domain;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application.Attributes;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data;

namespace Binsera.Application.Pool.Services
{
    /// <summary>
    /// Places an activated member into the Global Auto Pool and applies everything that follows.
    ///
    /// The decision of who gets paid lives in <see cref="PoolCascade"/> -- pure arithmetic, tested
    /// against the requirements doc's worked scenario. This class is the plumbing around it:
    /// allocate the position, write the placement, persist the effects the cascade returns.
    ///
    /// Placement and the whole cascade run in one serializable transaction behind a named
    /// application lock. The lock is deliberately global rather than per-row: two members placed at
    /// the same instant must not take the same position, and two cascades must not interleave on
    /// the same member's counters. At a Rs.200 joining fee, serialising this costs nothing next to
    /// the cost of getting it wrong.
    /// </summary>
    [AutoRegister(includeInterfaces: true)]
    public class PoolPlacementService(BinseraDbContext dbContext, ILogger logger) : IPoolPlacementService
    {
        private const string PlacementLockName = "GlobalAutoPool_Placement";
        private const int PlacementLockTimeoutMs = 30_000;

        /// <summary>Depths 1..8 -- the seven seeds plus the step that runs off the top of the chain.</summary>
        private const int UplinePathDepth = 8;

        public async Task PlaceMemberAsync(long memberId, CancellationToken cancellationToken = default)
        {
            var member = await dbContext.Set<Member>().FirstOrDefaultAsync(x => x.Id == memberId, cancellationToken);

            if (member is null)
            {
                logger.Warning("Pool placement skipped: member {MemberId} not found.", memberId);
                return;
            }

            // Pool entry follows payment and nothing else -- ConfPayNo is set when an admin approves
            // the joining order.
            if (member.ConfPayNo != 1)
            {
                logger.Information("Pool placement skipped: member {MemberId} is not active.", memberId);
                return;
            }

            if (member.IsPlacedInPool)
            {
                logger.Information("Pool placement skipped: member {MemberId} is already at position {Position}.",
                    memberId, member.PPositionM);
                return;
            }

            var levels = await LoadLevelsAsync(cancellationToken);

            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                await AcquirePlacementLockAsync(cancellationToken);

                // Re-read under the lock: another placement may have run between the check above
                // and the lock being granted.
                await dbContext.Entry(member).ReloadAsync(cancellationToken);
                if (member.IsPlacedInPool)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.Information("Pool placement skipped: member {MemberId} was placed concurrently.", memberId);
                    return;
                }

                var position = await AllocatePositionAsync(cancellationToken);
                var context = await PlacementContext.CreateAsync(dbContext, position, cancellationToken);

                Place(member, position, context);
                WriteUplinePath(member.RegNo!.Value, position, context);

                var effects = PoolCascade.PlanJoin(position, levels, context.StateOf);
                Apply(effects, context);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.Information(
                    "Member {MemberId} (RegNo {RegNo}) placed at pool position {Position}; {EffectCount} effects applied.",
                    memberId, member.RegNo, position, effects.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.Error(ex, "Pool placement FAILED for member {MemberId}.", memberId);
                throw;
            }
        }

        private async Task<IReadOnlyDictionary<int, PoolLevelSettings>> LoadLevelsAsync(CancellationToken cancellationToken)
        {
            var rows = await dbContext.Set<PoolLevelMaster>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                x => x.LevelNo,
                x => new PoolLevelSettings(x.LevelNo, x.RequiredMembers, x.ContributionPerMember, x.TransferAmount, x.StepsUp));
        }

        private Task AcquirePlacementLockAsync(CancellationToken cancellationToken)
            => dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {1}",
                [PlacementLockName, PlacementLockTimeoutMs],
                cancellationToken);

        /// <summary>
        /// The next position is one past the highest already assigned. The seed accounts carry
        /// position 0 and so are excluded.
        ///
        /// This reads MAX rather than COUNT deliberately. The two are equal -- positions are
        /// contiguous from 1, and the whole placement is one transaction so no gaps can form -- but
        /// MAX over the index on PPositionM is a seek to the end of it, while COUNT has to walk
        /// every placed member. At a million members that is the difference between a constant-time
        /// lookup and a scan on every single join. The index itself is created by
        /// src/DatabaseQuery/DbQuery/AddGlobalAutoPoolIndexes.sql, since member_Master is excluded
        /// from EF migrations.
        /// </summary>
        private async Task<long> AllocatePositionAsync(CancellationToken cancellationToken)
        {
            var highestAssigned = await dbContext.Set<Member>()
                .Where(x => x.PPositionM != null && x.PPositionM > 0)
                .MaxAsync(x => (long?)x.PPositionM, cancellationToken) ?? 0;

            return PoolTreePosition.NextPosition(highestAssigned);
        }

        private static void Place(Member member, long position, PlacementContext context)
        {
            var depth = PoolTreePosition.DepthOf(position);

            long parentRegNo;
            char side;

            if (PoolTreePosition.ParentOf(position) is long parentPosition)
            {
                parentRegNo = context.RequireRegNoAt(parentPosition);
                side = PoolTreePosition.SideOf(position) == PoolSide.Left ? 'L' : 'R';
            }
            else
            {
                // The very first member hangs off X1, the bottom of the seed chain.
                parentRegNo = context.SeedRegNo(1);
                side = 'L';
            }

            member.PlaceInPool(position, parentRegNo, side, depth);
            context.Register(position, member.RegNo!.Value);
        }

        private void WriteUplinePath(long regNo, long position, PlacementContext context)
        {
            for (var depth = 1; depth <= UplinePathDepth; depth++)
            {
                var ancestor = PoolTreePosition.ResolveStepsUp(position, depth);

                long? ancestorRegNo = ancestor.Kind switch
                {
                    PoolRecipientKind.Member => context.RegNoAt(ancestor.MemberPosition),
                    PoolRecipientKind.SeedAccount => context.SeedRegNo(ancestor.SeedIndex),
                    _ => null
                };

                // Above X7 there is no ancestor to record, and nothing deeper either.
                if (ancestorRegNo is null)
                    break;

                dbContext.Set<UplinePath>().Add(UplinePath.Create(regNo, ancestorRegNo.Value, depth));
            }
        }

        private void Apply(IReadOnlyList<PoolEffect> effects, PlacementContext context)
        {
            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case PoolEffectKind.UpgradeWalletCredit:
                    {
                        var regNo = context.RequireRegNoAt(effect.MemberPosition!.Value);
                        var sourceRegNo = effect.SourcePosition.HasValue
                            ? context.RegNoAt(effect.SourcePosition.Value)
                            : null;

                        dbContext.Set<PoolWalletTransaction>().Add(PoolWalletTransaction.CreditTo(
                            regNo, PoolWalletType.Upgrade, effect.Amount, effect.LevelNo, sourceRegNo,
                            effect.IdempotencyKey, $"Level {effect.LevelNo} contribution from RegNo {sourceRegNo}"));

                        context.BumpLevelCounter(regNo, effect.LevelNo);
                        break;
                    }

                    case PoolEffectKind.UpgradeWalletDebit:
                    {
                        var regNo = context.RequireRegNoAt(effect.MemberPosition!.Value);

                        dbContext.Set<PoolWalletTransaction>().Add(PoolWalletTransaction.DebitFrom(
                            regNo, PoolWalletType.Upgrade, effect.Amount, effect.LevelNo,
                            effect.IdempotencyKey, $"Level {effect.LevelNo} upgrade"));
                        break;
                    }

                    case PoolEffectKind.MainWalletCredit:
                    {
                        var regNo = context.RequireRegNoAt(effect.MemberPosition!.Value);

                        dbContext.Set<PoolWalletTransaction>().Add(PoolWalletTransaction.CreditTo(
                            regNo, PoolWalletType.Main, effect.Amount, effect.LevelNo, null,
                            effect.IdempotencyKey, $"Level {effect.LevelNo} income"));

                        // Mirrored into the legacy wallet, which is what withdrawals and the
                        // member's transaction history read. Written directly rather than through
                        // sp_IA2 -- see MemberAccount.Credited.
                        dbContext.Set<MemberAccount>().Add(MemberAccount.Credited(
                            regNo, effect.Amount, "PoolLevelIncome", $"Global Auto Pool level {effect.LevelNo} income"));

                        context.RecordLevelIncome(regNo, effect.LevelNo, effect.Amount);
                        break;
                    }

                    case PoolEffectKind.CompanyWalletCredit:
                    {
                        var sourceRegNo = context.RequireRegNoAt(effect.SourcePosition!.Value);
                        var destination = effect.SeedIndex is int seed ? $"seed X{seed}" : "above the seed chain";
                        var remark = $"Global Auto Pool level {effect.LevelNo} transfer from RegNo {sourceRegNo} to {destination}";

                        dbContext.Set<PoolWalletTransaction>().Add(PoolWalletTransaction.ToCompany(
                            effect.Amount, effect.LevelNo, sourceRegNo, effect.SeedIndex, effect.IdempotencyKey, remark));

                        dbContext.Set<CompanyAccount>().Add(CompanyAccount.Collect(effect.Amount, "PoolTransfer", remark));
                        break;
                    }

                    case PoolEffectKind.LevelCompleted:
                    {
                        var regNo = context.RequireRegNoAt(effect.MemberPosition!.Value);
                        context.MarkLevelComplete(regNo, effect.LevelNo);
                        break;
                    }
                }
            }
        }
    }
}
