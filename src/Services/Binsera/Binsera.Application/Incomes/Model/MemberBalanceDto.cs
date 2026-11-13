namespace Binsera.Application.Incomes.Model
{
    public sealed class MemberBalanceDto
    {
        public long RegNo { get; set; }

        // Income
        public decimal BinaryIncome { get; set; }
        public decimal LevelIncome { get; set; }
        public decimal Deposit { get; set; }
        public decimal RepurchaseIncome { get; set; }
        public decimal DirectIncome { get; set; }
        public decimal RewardIncome { get; set; }
        public decimal DirectReferralIncome { get; set; }
        public decimal PerformanceIncome { get; set; }
        public decimal TotalIn { get; set; }

        // Deductions
        public decimal Deduct { get; set; }
        public decimal TDS { get; set; }
        public decimal HC { get; set; }
        public decimal Withdraw { get; set; }
        public decimal TDSWP { get; set; }
        public decimal ProductOrder { get; set; }
        public decimal TotalOut { get; set; }

        // Balance
        public decimal LockAmt { get; set; }
        public decimal TotalAvail { get; set; }
        public decimal TotalLedAccount { get; set; }
    }
}