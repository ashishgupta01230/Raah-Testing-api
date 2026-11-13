using Binsera.Core.OutBox.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.OutBox
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.Property(x => x.MessageType)
            .IsRequired()
            .HasMaxLength(200);

            builder.Property(x => x.Payload)
                .IsRequired();

            builder.Property(x => x.Processed).HasDefaultValue(false);
            builder.Property(x => x.DeadLetter).HasDefaultValue(false);
            builder.Property(x => x.Attempts).HasDefaultValue(0);

            // optional indexes
            builder.HasIndex(x => new { x.Processed, x.Attempts });
        }
    }
}
