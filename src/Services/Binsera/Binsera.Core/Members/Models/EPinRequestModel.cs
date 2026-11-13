using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Members.Models
{
    public class EPinRequestModel
    {
        public long Id { get; set; }

        public string RequestNo { get; set; } = string.Empty;

        public long MemberId { get; set; }

        public long KitId { get; set; }

        public int Quantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime RequestedOn { get; set; }

        public long? ApprovedBy { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public string? RejectedReason { get; set; }
    }
}
