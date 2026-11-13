using Binsera.Core.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Audit
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(100);
            builder.Property(x => x.PerformedByName).HasMaxLength(200);
            builder.Property(x => x.Metadata).HasMaxLength(2000);

            builder.HasIndex(x => x.EventType);
            builder.HasIndex(x => x.OccurredOn);
            builder.HasIndex(x => x.RelatedEntityId);
            builder.HasIndex(x => x.EntityType);
        }
    }
}
