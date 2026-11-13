using Binsera.Core.Master.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Persistence.Configurations.Master
{
    public class StateConfiguration : IEntityTypeConfiguration<StateView>
    {
        public void Configure(EntityTypeBuilder<StateView> builder)
        {
            builder.ToTable("STATE");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("SID").HasColumnType("int");
            builder.Property(x => x.StateName).HasColumnName("StateName");
            builder.Property(x => x.CID).HasColumnName("CID");
            builder.Property(x => x.sActive).HasColumnName("sActive");
            builder.Ignore(x => x.IsDeleted);
        }
    }
}
