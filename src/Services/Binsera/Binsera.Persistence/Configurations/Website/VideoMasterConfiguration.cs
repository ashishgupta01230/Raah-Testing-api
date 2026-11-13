using Binsera.Core.Website.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Website
{
    public class VideoMasterConfiguration : IEntityTypeConfiguration<VideoMaster>
    {
        public void Configure(EntityTypeBuilder<VideoMaster> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Description)
                .HasMaxLength(2000);

            builder.Property(x => x.YouTubeEmbedUrl)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            //builder.Ignore(x => x.IsDeleted);

            builder.HasOne(x => x.VideoCategory)
                .WithMany()
                .HasForeignKey(x => x.VideoCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.VideoCategoryId);
        }
    }
}
