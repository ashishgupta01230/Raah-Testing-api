using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class MemberDocumentConfiguration : IEntityTypeConfiguration<MemberDocument>
    {
        public void Configure(EntityTypeBuilder<MemberDocument> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MemberId)
                .IsRequired();

            builder.Property(x => x.DocumentId)
                .IsRequired();

            builder.Property(x => x.MediaId)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.RejectionReason)
                .HasMaxLength(500);

            builder.HasOne(x => x.Document)
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.MemberId, x.DocumentId });
            builder.HasIndex(x => x.Status);
        }
    }
}
