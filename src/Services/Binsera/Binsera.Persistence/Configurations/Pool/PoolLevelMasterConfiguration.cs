using Binsera.Core.Pool.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Pool
{
    public class PoolLevelMasterConfiguration : IEntityTypeConfiguration<PoolLevelMaster>
    {
        // HasData needs a stable value; DateTime.Now here would make the model differ on every
        // scaffold and produce an empty migration each time.
        private static readonly DateTime SeededOn = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public void Configure(EntityTypeBuilder<PoolLevelMaster> builder)
        {
            builder.ToTable("PoolLevelMaster");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.LevelNo).IsRequired();
            builder.Property(x => x.RequiredMembers).IsRequired();
            builder.Property(x => x.ContributionPerMember).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.TransferAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.StepsUp);
            builder.Property(x => x.IsActive).IsRequired();

            // IncomingTotal and MainWalletIncome are computed on the entity, not columns.
            builder.Ignore(x => x.IncomingTotal);
            builder.Ignore(x => x.MainWalletIncome);

            builder.HasIndex(x => x.LevelNo).IsUnique();

            // Amounts from Global_Auto_Pool_Scenario_Complete_Flow.md PART F, plus the level 0
            // joining row from Global_Auto_Pool_UI_Application_Design.md PART 12.
            //
            // Two invariants hold across every row, and both are worth re-checking by hand before
            // changing any number here:
            //   IncomingTotal   = RequiredMembers x ContributionPerMember
            //   MainWalletIncome = IncomingTotal - TransferAmount
            // and the levels chain: level N's TransferAmount is level N+1's ContributionPerMember.
            builder.HasData(
                Seed(1, levelNo: 0, required: 1, contribution: 15m, transfer: 15m, stepsUp: 1),
                Seed(2, levelNo: 1, required: 2, contribution: 15m, transfer: 30m, stepsUp: 2),
                Seed(3, levelNo: 2, required: 4, contribution: 30m, transfer: 110m, stepsUp: 3),
                Seed(4, levelNo: 3, required: 8, contribution: 110m, transfer: 860m, stepsUp: 4),
                Seed(5, levelNo: 4, required: 16, contribution: 860m, transfer: 12_760m, stepsUp: 5),
                Seed(6, levelNo: 5, required: 32, contribution: 12_760m, transfer: 1_08_320m, stepsUp: 6),
                Seed(7, levelNo: 6, required: 64, contribution: 1_08_320m, transfer: 1_00_000m, stepsUp: 7),
                Seed(8, levelNo: 7, required: 128, contribution: 1_00_000m, transfer: 0m, stepsUp: null));
        }

        private static object Seed(long id, int levelNo, int required, decimal contribution, decimal transfer, int? stepsUp)
            => new
            {
                Id = id,
                LevelNo = levelNo,
                RequiredMembers = required,
                ContributionPerMember = contribution,
                TransferAmount = transfer,
                StepsUp = stepsUp,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = SeededOn,
                CreatedBy = 0L
            };
    }
}
