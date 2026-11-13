using Binsera.Core.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Orders
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderNo).IsRequired().HasMaxLength(30);
            builder.Property(x => x.MemberId).IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Remarks).HasMaxLength(500);
            builder.Property(x => x.RejectedReason).HasMaxLength(500);

            builder.HasIndex(x => x.OrderNo).IsUnique();
            builder.HasIndex(x => x.MemberId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.OrderType);
            builder.HasIndex(x => x.CreatedDate);
        }
    }
}
