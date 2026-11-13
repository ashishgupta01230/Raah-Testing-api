using Binsera.Core.Enquiries.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Enquiries
{
    public class EnquiryConfiguration : IEntityTypeConfiguration<Enquiry>
    {
        public void Configure(EntityTypeBuilder<Enquiry> builder)
        {
            // Primary key
            builder.HasKey(e => e.Id);

            // Columns
            builder.Property(e => e.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(e => e.Email)
                   .HasMaxLength(100);

            builder.Property(e => e.Phone)
                   .HasMaxLength(20);

            //builder.Property(e => e.City)
            //       .HasMaxLength(50);

            //builder.Property(e => e.State)
            //       .HasMaxLength(50);

            //builder.Property(e => e.ZipCode)
            //       .HasMaxLength(10);

            builder.Property(e => e.Remarks)
                   .HasMaxLength(500);

            builder.Property(e => e.EnquiryType).IsRequired();
            builder.Property(e => e.Status).IsRequired();
        }
    }

}
