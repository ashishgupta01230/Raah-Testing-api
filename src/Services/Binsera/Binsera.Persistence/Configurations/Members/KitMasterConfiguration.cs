using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class KitMasterConfiguration : IEntityTypeConfiguration<KitMaster>
    {
        public void Configure(EntityTypeBuilder<KitMaster> builder)
        {
            builder.ToTable("Kit_Master");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("kid")
                   .HasColumnType("int");

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
