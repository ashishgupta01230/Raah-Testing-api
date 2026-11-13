using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class CompanyAccountConfiguration : IEntityTypeConfiguration<CompanyAccount>
    {
        public void Configure(EntityTypeBuilder<CompanyAccount> builder)
        {
            builder.ToTable("Company_account");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ACID");
            builder.Property(x => x.TransDate).HasColumnName("transDate");
            builder.Property(x => x.Credit).HasColumnName("credit").HasColumnType("money");
            builder.Property(x => x.Debit).HasColumnName("debit").HasColumnType("money");
            builder.Property(x => x.TransType).HasColumnName("transType");
            builder.Property(x => x.Remark).HasColumnName("Remark");
            builder.Property(x => x.TrStatus).HasColumnName("trStatus");

            // Pre-existing legacy table -- EF does not own its schema.
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
