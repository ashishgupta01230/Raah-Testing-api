using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Model
{
    public class CityModel 
    {
        public int CTID { get; set; }
        public int CID { get; set; }
        public string CountryName { get; set; }
        public int SID { get; set; }
        public string StateName { get; set; }
        public int DistId { get; set; }
        public string DistrictName { get; set; }
        public string CityName { get; set; }
        public string Pincode { get; set; }
        public bool Active { get; set; }

    }

    public class UpdateCityRequest
    {
        public string cityName { get; set; }
        public string pincode { get; set; }
        public bool active { get; set; }
    }

    public class CityFilterModel : PaginationModel
    {
        public string? Keyword { get; set; }
        public int? CID { get; set; }
        public int? SID { get; set; }
        public int? DistId { get; set; }
        public bool? Active { get; set; }
    }
}
