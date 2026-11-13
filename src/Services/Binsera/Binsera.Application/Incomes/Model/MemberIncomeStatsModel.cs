namespace Binsera.Application.Incomes.Model
{
    public class MemberIncomeStatsModel
    {
        public decimal WalletBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TeamBussiness { get; set; }
        public int TotalTeamMembers { get; set; }
        public int DirectMembers { get; internal set; }
        public decimal DirectReferralIncome { get; set; }
        public decimal PerformanceIncome { get; set; }
    }
}