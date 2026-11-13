using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Model
{
    public class CompanyBankAccountModel
    {
        public string AccountNo { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string IFSCCode { get;  set; } = string.Empty;
        public string Address { get;  set; } = string.Empty;
        public int paymentTypeId { get; set; }
    }
}
