using HautSphere.BuildingBlocks.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Domain
{
    public class CompanyBankAccount : DomainBase
    {
        public string AccountNo { get;private set; } = string.Empty;

        public string BankName { get;private set; } = string.Empty;
        public string BranchName { get;private set; } = string.Empty;

        public string IFSCCode { get;private set; } = string.Empty;

        public string Address { get;private set; } = string.Empty;

        public int PaymentTypeId { get; private set; } 
    }
}
