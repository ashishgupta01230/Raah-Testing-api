using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Members
{
    public class MemberAdditionalInfoConfiguration : IEntityTypeConfiguration<MemberAdditionalInfo>
    {
        public void Configure(EntityTypeBuilder<MemberAdditionalInfo> builder)
        {
            builder.Property(e => e.MemberInfo).HasColumnType("NVARCHAR(MAX)");
            builder.HasOne<Member>().WithOne(s=>s.MemberAdditionalInfo).HasForeignKey<MemberAdditionalInfo>(f=>f.Id);
        }
    }
}
