using Binsera.Core.Pool.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Pool
{
    public class PoolSeedAccountConfiguration : IEntityTypeConfiguration<PoolSeedAccount>
    {
        private static readonly DateTime SeededOn = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public void Configure(EntityTypeBuilder<PoolSeedAccount> builder)
        {
            builder.ToTable("PoolSeedAccount");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SeedIndex).IsRequired();
            builder.Property(x => x.MemberRegNo).IsRequired();

            builder.HasIndex(x => x.SeedIndex).IsUnique();
            builder.HasIndex(x => x.MemberRegNo).IsUnique();

            // X1..X7 occupy member_Master rows 1..7, created by
            // src/DatabaseQuery/DbQuery/SeedGlobalAutoPoolSeedAccounts.sql. Seed index N maps to
            // RegNo N; that script and this seed have to be applied together.
            builder.HasData(
                Enumerable.Range(1, 7).Select(seedIndex => new
                {
                    Id = (long)seedIndex,
                    SeedIndex = seedIndex,
                    MemberRegNo = (long)seedIndex,
                    IsDeleted = false,
                    CreatedDate = SeededOn,
                    CreatedBy = 0L
                }).ToArray());
        }
    }
}
