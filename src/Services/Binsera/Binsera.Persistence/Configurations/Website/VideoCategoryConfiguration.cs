using Binsera.Core.Website.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Website
{
    public class VideoCategoryConfiguration : IEntityTypeConfiguration<VideoCategory>
    {
        public void Configure(EntityTypeBuilder<VideoCategory> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CategoryName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);
        }
    }
}
