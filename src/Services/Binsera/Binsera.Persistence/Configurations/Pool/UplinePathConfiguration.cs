using Binsera.Core.Pool.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Pool
{
    public class UplinePathConfiguration : IEntityTypeConfiguration<UplinePath>
    {
        public void Configure(EntityTypeBuilder<UplinePath> builder)
        {
            builder.ToTable("UplinePath");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MemberRegNo).IsRequired();
            builder.Property(x => x.AncestorRegNo).IsRequired();
            builder.Property(x => x.Depth).IsRequired();

            // The doc's lookup shape: "Index (MemberId, Depth)".
            builder.HasIndex(x => new { x.MemberRegNo, x.Depth }).IsUnique();

            // The reverse direction -- everyone below a given member -- is what downline counts read.
            builder.HasIndex(x => x.AncestorRegNo);
        }
    }
}
