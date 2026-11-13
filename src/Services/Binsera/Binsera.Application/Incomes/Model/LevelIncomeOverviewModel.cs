namespace Binsera.Application.Incomes.Model
{
    public class LevelIncomeOverviewModel
    {
        public int Level { get; set; }

        /// <summary>
        /// Total Members on this level
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Total Income earned from this level
        /// </summary>
        public decimal Income { get; set; }
    }
}