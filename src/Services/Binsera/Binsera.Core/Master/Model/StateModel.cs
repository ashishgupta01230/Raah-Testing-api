using HautSphere.BuildingBlocks.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Model
{
    
        public class StateModel
        {
            public int SID { get; set; }
            public string StateName { get; set; }
            public int CID { get; set; }
            public string CountryName { get; set; }
            public bool sActive { get; set; }
        }

    public class UpdateStateStatus
    {
        public bool sActive { get; set; }
    }

    public class StateFilterModel : PaginationModel
    {
        public string? Keyword { get; set; }
        public int? CID { get; set; }
        public bool? sActive { get; set; }
    }

}
