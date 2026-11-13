using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Master.Model
{
    public class CountryModel
    {
        public int CID { get; set; }
        public string CountryName { get; set; }
        public bool cActive { get; set; }
    }

    public class UpdateCountryStatus
    {
        public bool cActive { get; set; }
    }

    public class CountryFilterModel : PaginationModel
    {
        public string Keyword { get; set; }
        public bool? cActive { get; set; }
    }
}
