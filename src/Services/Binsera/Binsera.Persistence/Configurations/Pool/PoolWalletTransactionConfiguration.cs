using Binsera.Core.Pool.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Pool
{
    public class PoolWalletTransactionConfiguration : IEntityTypeConfiguration<PoolWalletTransaction>
    {
        public void Configure(EntityTypeBuilder<PoolWalletTransaction> builder)
        {
            builder.ToTable("PoolWalletTransaction");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Credit).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.Debit).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.WalletType).IsRequired();
            builder.Property(x => x.LevelNo).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Remark).HasMaxLength(400);
            builder.Property(x => x.OccurredOn).IsRequired();

            // The real double-payment guard. The service checks for an existing key first, but a
            // concurrent replay can pass that check and still lose here -- which is the point.
            // Scoped by wallet type because one level completion legitimately writes an Upgrade
            // debit and a Main credit that share a key.
            builder.HasIndex(x => new { x.IdempotencyKey, x.WalletType }).IsUnique();

            builder.HasIndex(x => new { x.MemberRegNo, x.WalletType });
            builder.HasIndex(x => x.OccurredOn);
        }
    }
}
