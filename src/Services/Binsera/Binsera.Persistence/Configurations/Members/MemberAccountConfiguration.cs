using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class MemberAccountConfiguration : IEntityTypeConfiguration<MemberAccount>
    {
        public void Configure(EntityTypeBuilder<MemberAccount> builder)
        {
            builder.ToTable("Member_Account");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("ACID");

            builder.Property(x => x.RegNo).HasColumnName("regno").HasColumnType("int"); ;
            builder.Property(x => x.TransType).HasColumnName("transType");

            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
