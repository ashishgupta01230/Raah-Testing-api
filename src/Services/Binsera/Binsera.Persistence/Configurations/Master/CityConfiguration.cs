using Binsera.Core.Master.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Binsera.Persistence.Configurations.Master
{
    public class CityConfiguration : IEntityTypeConfiguration<CityView>
    {
        public void Configure(EntityTypeBuilder<CityView> builder)
        {
            builder.ToTable("CITY");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("CTID")
                .HasColumnType("int")
                .ValueGeneratedOnAdd();

            builder.Property(c => c.CID)
                .HasColumnName("CID").HasColumnType("int").IsRequired();

            builder.Property(c => c.SID)
                .HasColumnName("SID").HasColumnType("int").IsRequired();

            builder.Property(c => c.DistId)
                .HasColumnName("distID").HasColumnType("int").IsRequired();

            builder.Property(c => c.CityName)
                .HasColumnName("cityName").HasMaxLength(200).IsRequired();

            builder.Property(c => c.Pincode)
                .HasColumnName("Pincode").HasMaxLength(20);

            builder.Property(c => c.Active)
                .HasColumnName("Active").HasDefaultValue(true);

            builder.Ignore(d => d.IsDeleted);
            //builder.Ignore(d => d.CountryName);
            //builder.Ignore(d => d.StateName);
        }
    }
}

