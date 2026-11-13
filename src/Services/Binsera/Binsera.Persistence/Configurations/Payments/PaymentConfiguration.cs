using Binsera.Core.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Payments
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PaymentNo).IsRequired().HasMaxLength(30);
            builder.Property(x => x.MemberId).IsRequired();
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TransactionReference).HasMaxLength(200);
            builder.Property(x => x.GatewayTransactionId).HasMaxLength(200);
            builder.Property(x => x.Remarks).HasMaxLength(500);

            builder.HasIndex(x => x.PaymentNo).IsUnique();
            builder.HasIndex(x => x.MemberId);
            builder.HasIndex(x => x.ReferenceType);
            builder.HasIndex(x => x.ReferenceId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedDate);
            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        }
    }
}
