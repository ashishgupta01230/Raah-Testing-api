using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class LevelSlabConfiguration : IEntityTypeConfiguration<LevelSlab>
    {
        public void Configure(EntityTypeBuilder<LevelSlab> builder)
        {
            builder.HasNoKey();
            builder.ToTable("Level_Slab");

            builder.Property(x => x.Levels).HasColumnName("levels").HasColumnType("int");
            builder.Property(x => x.LevelPerIdRate).HasColumnName("levelperidrate").HasColumnType("decimal(18,2)");
            builder.Property(x => x.IsActive).HasColumnName("IsActive");
            builder.Property(x => x.CommissionType).HasColumnName("CommissionType").HasColumnType("varchar(50)");

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
