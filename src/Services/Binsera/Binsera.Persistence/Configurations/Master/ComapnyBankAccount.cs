using Binsera.Core.Master.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Persistence.Configurations.Master
{
    public class CompanyBankAccountConfiguration : IEntityTypeConfiguration<CompanyBankAccount>
    {
        public void Configure(EntityTypeBuilder<CompanyBankAccount> builder)
        {
            builder.ToTable("COMPANYBANKACCOUNT");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccountNo)
                   .HasMaxLength(50);

            builder.Property(x => x.BankName)
                   .HasMaxLength(100);

            builder.Property(x => x.BranchName)
                   .HasMaxLength(100);

            builder.Property(x => x.IFSCCode)
                   .HasMaxLength(20);

            builder.Property(x => x.Address)
                   .HasMaxLength(500);

            builder.Property(x => x.PaymentTypeId)
                   .IsRequired();
        }
    }
}
