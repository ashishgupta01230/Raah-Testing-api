using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.ToTable("member_Master");
            builder.ToTable(tb => tb.UseSqlOutputClause(false));
            builder.Metadata.SetIsTableExcludedFromMigrations(true);
        }
    }
}
