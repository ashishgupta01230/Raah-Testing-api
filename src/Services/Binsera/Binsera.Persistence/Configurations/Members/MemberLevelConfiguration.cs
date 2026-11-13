using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class MemberLevelConfiguration : IEntityTypeConfiguration<MemberLevel>
    {
        public void Configure(EntityTypeBuilder<MemberLevel> builder)
        {
            builder.ToTable("Member_Level");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("MLID")
                   .HasColumnType("int");

            builder.Property(x => x.RegNo).HasColumnName("RegNo").HasColumnType("numeric"); ;
            builder.Property(x => x.Levels).HasColumnName("Levels").HasColumnType("int");
            builder.Property(x => x.TlCount).HasColumnName("tlCount").HasColumnType("int");
            builder.Property(x => x.RlCount).HasColumnName("rlCount").HasColumnType("int");
            builder.Property(x => x.TlIncome).HasColumnName("tlIncome").HasColumnType("decimal(18,2)");
            builder.Property(x => x.RlIncome).HasColumnName("rlIncome").HasColumnType("decimal(18,2)");
            builder.Property(x => x.MlActive).HasColumnName("mlActive");

            // Legacy table, no IsDeleted column -- opt out of DomainBase's global soft-delete
            // convention (BinseraDbContext.OnModelCreating), otherwise every query against this
            // table fails with "Invalid column name 'IsDeleted'".
            builder.Ignore(x => x.IsDeleted);

            // No HasIndex here: the table is excluded from migrations, so declaring one would tell
            // the model an index exists that EF will never create. The pool reads this table by
            // (RegNo, Levels) on every placement and the table had no indexes at all, so that one
            // is created by src/DatabaseQuery/DbQuery/AddGlobalAutoPoolIndexes.sql instead.
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
