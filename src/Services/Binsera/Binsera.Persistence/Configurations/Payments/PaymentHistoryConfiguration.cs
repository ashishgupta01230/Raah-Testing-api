using Binsera.Core.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Payments
{
    public class PaymentHistoryConfiguration : IEntityTypeConfiguration<PaymentHistory>
    {
        public void Configure(EntityTypeBuilder<PaymentHistory> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Remarks).HasMaxLength(500);

            builder.HasOne(x => x.Payment)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.PaymentId, x.ChangedOn });
        }
    }
}
