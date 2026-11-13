using Binsera.Core.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Binsera.Persistence.Configurations.Auth
{
    public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
    {
        public void Configure(EntityTypeBuilder<UserLogin> builder)
        {
            builder.Property(p => p.UserName).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Password).IsRequired().HasMaxLength(20);
            builder.Property(p => p.Email).IsRequired().HasMaxLength(200);
            builder.Property(p => p.RoleId).IsRequired();
            builder.HasIndex(s => s.UserName).IsUnique();
        }
    }
    public class UserLogConfiguration : IEntityTypeConfiguration<UserLog>
    {
        public void Configure(EntityTypeBuilder<UserLog> builder)
        {
            builder.HasOne<UserLogin>().WithMany().HasForeignKey(p => p.UserId);
            builder.Property(p => p.UserName).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Email).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Browser).HasMaxLength(500);
            builder.Property(p => p.ClientIp).HasMaxLength(50);
            builder.Property(p => p.RefreshTokenHash).HasMaxLength(256);
            builder.Property(p => p.PreviousRefreshTokenHash).HasMaxLength(256);
        }
    }
}
