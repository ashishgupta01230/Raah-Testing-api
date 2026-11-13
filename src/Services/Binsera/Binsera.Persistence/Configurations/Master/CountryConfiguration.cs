using Binsera.Core.Master.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Master
{
    public class CountryConfiguration : IEntityTypeConfiguration<CountryView>
    {
        public void Configure(EntityTypeBuilder<CountryView> builder)
        {
            builder.ToTable("COUNTRY");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("CID").HasColumnType("int");
            builder.Property(x => x.CountryName).HasColumnName("CountryName");
            builder.Property(x => x.cActive).HasColumnName("cActive");
            builder.Ignore(x => x.IsDeleted);
        }
    }
}
