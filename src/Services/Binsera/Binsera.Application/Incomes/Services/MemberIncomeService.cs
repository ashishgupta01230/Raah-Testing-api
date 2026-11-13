using Binsera.Application.Incomes.Contracts;
using Binsera.Application.Incomes.Model;
using Binsera.Application.Members;
using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Incomes.Services
{
    /// <summary>
    /// These four endpoints predate the Global Auto Pool pivot and read commission engines that
    /// have since been removed -- first the Course Commission engine (Member_Level,
    /// Member_level_JoinDetails), then Phase 1's direct referral income and Phase 2's
    /// Business/Commission engine (CommissionLine/CommissionRun). None of them write anything any more.
    ///
    /// The routes and response shapes are kept so the frontend keeps working, but the level and
    /// team income endpoints now return empty/zeroed results until Global Auto Pool defines its
    /// own income model. GetOverviewIncomeAmount still returns real numbers -- it reads the
    /// Member_Account ledger directly, which the pool's Main Wallet will keep writing to.
    /// </summary>
    [AutoRegister(includeInterfaces: true)]
    public class MemberIncomeService(
        IUnitOfWork unitOfWork,
        IGraphDatabaseMemberQueryService graphDatabaseMemberQueryService) : IMemberIncomeService
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        private readonly IRepository<MemberAccount> _memberAccountRepository = unitOfWork.Repository<MemberAccount>();

        public Task<LevelIncomeOverviewModel[]> GetLevelIncomeOverview(Guid memberId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.Empty<LevelIncomeOverviewModel>());
        }

        public Task<MemberTeamIncomeSummaryModel> GetTeamIncomeSummary(Guid memberId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MemberTeamIncomeSummaryModel { TotalTeamIncome = 0 });
        }

        public Task<TeamIncomeActivityPageModel> GetTeamIncomeActivity(Guid memberId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 20 : pageSize;

            return Task.FromResult(new TeamIncomeActivityPageModel
            {
                Page = safePage,
                PageSize = safePageSize,
                TotalRecords = 0,
                TotalPages = 0,
                Items = []
            });
        }

        public async Task<MemberIncomeStatsModel> GetOverviewIncomeAmount(Guid memberId, CancellationToken cancellationToken)
        {
            var regNo = await GetRegNo(memberId, cancellationToken);

            var balance = await _memberAccountRepository.TableNoTracking
            .Where(x => x.RegNo == regNo)
            .GroupBy(x => x.RegNo)
            .Select(g => new MemberBalanceDto
            {
                RegNo = g.Key,
                BinaryIncome = g.Sum(x => x.TransType == "BinaryIncome" && x.TrStatus == 1 ? x.Credit : 0),
                LevelIncome = g.Sum(x => x.TransType == "LevelIncome" && x.TrStatus == 1 ? x.Credit : 0),
                Deposit = g.Sum(x => x.TransType == "Deposit" && x.TrStatus == 1 ? x.Credit : 0),
                RepurchaseIncome = g.Sum(x => x.TransType == "RepurchaseIncome" && x.TrStatus == 1 ? x.Credit : 0),
                DirectIncome = g.Sum(x => x.TransType == "DirectIncome" && x.TrStatus == 1 ? x.Credit : 0),
                RewardIncome = g.Sum(x => x.TransType == "Reward" && x.TrStatus == 1 ? x.Credit : 0),
                DirectReferralIncome = g.Sum(x => x.TransType == "DirectReferralIncome" && x.TrStatus == 1 ? x.Credit : 0),
                PerformanceIncome = g.Sum(x => x.TransType == "PerformanceIncome" && x.TrStatus == 1 ? x.Credit : 0),
                TotalIn = g.Sum(x =>
                    x.TrStatus == 1 &&
                    (x.TransType == "BinaryIncome" ||
                     x.TransType == "LevelIncome" ||
                     x.TransType == "Deposit" ||
                     x.TransType == "DirectIncome" ||
                     x.TransType == "RepurchaseIncome" ||
                     x.TransType == "Reward" ||
                     x.TransType == "DirectReferralIncome" ||
                     x.TransType == "PerformanceIncome")? x.Credit : 0),

                Deduct = g.Sum(x => x.TransType == "Deduct" && x.TrStatus == 1 ? x.Debit : 0),
                TDS = g.Sum(x => x.TransType == "TDS" && x.TrStatus == 1 ? x.Debit : 0),
                HC = g.Sum(x => x.TransType == "HandlingCharge" && x.TrStatus == 1 ? x.Debit : 0),
                Withdraw = g.Sum(x =>
                    x.TransType == "Withdraw" &&
                    x.CRDB == 'D' &&
                    (x.TrStatus == 1 || x.TrStatus == 2)
                        ? x.Debit : 0),

                TDSWP = g.Sum(x => x.TransType == "TDSWP" && x.TrStatus == 1 ? x.Debit : 0),
                ProductOrder = g.Sum(x => x.TransType == "OrderPayment" && x.TrStatus == 1 ? x.Debit : 0),

                TotalOut = g.Sum(x =>
                    x.TrStatus == 1 &&
                    (x.TransType == "HandlingCharge" ||
                     x.TransType == "Deduct" ||
                     x.TransType == "TDS" ||
                     x.TransType == "TDSWP" ||
                     x.TransType == "Withdraw" ||
                     x.TransType == "OrderPayment")
                        ? x.Debit : 0),

                LockAmt =
                    g.Sum(x =>
                        x.TrStatus == 0 &&
                        (x.TransType == "BinaryIncome" ||
                         x.TransType == "LevelIncome" ||
                         x.TransType == "Deposit")
                            ? x.Credit : 0)
                    +
                    g.Sum(x =>
                        x.TrStatus == 0 &&
                        (x.TransType == "HandlingCharge" ||
                         x.TransType == "Deduct" ||
                         x.TransType == "TDS" ||
                         x.TransType == "TDSWP" ||
                         x.TransType == "Withdraw")
                            ? x.Debit : 0),

                TotalAvail =
                    g.Sum(x => (x.TrStatus == 0 || x.TrStatus == 1) && x.TransType != "Return" ? x.Credit : 0)
                    -
                    g.Sum(x => (x.TrStatus == 0 || x.TrStatus == 1) ? x.Debit : 0),

                TotalLedAccount =
                    g.Sum(x => x.TrStatus == 1 && x.TransType != "Return" ? x.Credit : 0)
                    -
                    g.Sum(x => x.TrStatus == 1 ? x.Debit : 0)
                    +
                    g.Sum(x => x.TrStatus == 0 && x.CRDB == 'C' ? x.Credit : 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

            var teams = await graphDatabaseMemberQueryService.GetMemberTeamSizeAsync(memberId);
            return new MemberIncomeStatsModel
            {
                TotalTeamMembers = teams.teamSize,
                DirectMembers = teams.directMembers,
                TotalIncome = balance?.TotalIn ?? 0,
                WalletBalance = balance?.TotalAvail ?? 0,
                DirectReferralIncome = balance?.DirectReferralIncome ?? 0,
                PerformanceIncome = balance?.PerformanceIncome ?? 0
            };
        }

        private async Task<long> GetRegNo(Guid memberId, CancellationToken cancellationToken)
        {
            var regNo = await _memberRepository.TableNoTracking.Where(x => x.MemberGraphId == memberId).Select(s => s.RegNo).FirstOrDefaultAsync(cancellationToken);
            return regNo ?? 0;
        }
    }
}
