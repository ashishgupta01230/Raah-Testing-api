using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Domain.Models;


namespace Binsera.Core.Master.Model
{
    public class DistrictModel 
    {
        public  int DistId { get; set; }
        public int CID { get; set; }
        public int SID { get; set; }
        public string DistName { get; set; }
        public string CountryName { get; set; }
        public string StateName { get; set; }
        public bool Active { get; set; }

        //public Country? Country { get; set; }
        //public State? State { get; set; }


    }

    public class UpdateDistrictStatus
    {
        public bool active { get; set; }
    }

    public class DistrictFilterModel : PaginationModel
    {
        public string? Keyword { get; set; }
        public int? CID { get; set; }
        public int? SID { get; set; }
        public bool? Active { get; set; }
    }
}
