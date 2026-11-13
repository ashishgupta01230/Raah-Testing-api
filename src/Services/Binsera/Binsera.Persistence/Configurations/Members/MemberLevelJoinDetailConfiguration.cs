using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class MemberLevelJoinDetailConfiguration : IEntityTypeConfiguration<MemberLevelJoinDetail>
    {
        public void Configure(EntityTypeBuilder<MemberLevelJoinDetail> builder)
        {
            builder.ToTable("Member_level_JoinDetails");

            builder.HasKey(x => x.Id);

            // Same MLID column MemberLevelConfiguration maps to its own Id -- here
            // it's a plain FK back to Member_Level, not unique per row (many
            // join-detail rows share one MLID), so treat this table as read-only.
            builder.Property(x => x.Id)
                   .HasColumnName("MLID")
                   .HasColumnType("int");

            builder.Property(x => x.ToRegNo).HasColumnName("toRegNo").HasColumnType("numeric");
            builder.Property(x => x.FromRegNo).HasColumnName("fromRegNo").HasColumnType("numeric");
            builder.Property(x => x.EntryDate).HasColumnName("entryDate").HasColumnType("datetime");
            builder.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");

            // Legacy table, no IsDeleted column -- opt out of DomainBase's global
            // soft-delete convention (BinseraDbContext.OnModelCreating), otherwise
            // every query fails with "Invalid column name 'IsDeleted'".
            builder.Ignore(x => x.IsDeleted);

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
