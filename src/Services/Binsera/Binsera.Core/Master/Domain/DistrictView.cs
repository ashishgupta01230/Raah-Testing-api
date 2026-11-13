using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Master.Domain
{
    public class DistrictView : DomainBase
    {
        //public int Id { get; set; }
        public int CID { get; set; }
        public int SID { get; set; }
        public string DistName { get; set; }
        public string CountryName { get; set; }
        public string StateName { get; set; }
        public bool Active { get; set; }
    }
}
