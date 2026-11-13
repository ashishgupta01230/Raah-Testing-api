using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Locations.Models
{
    public class UpdateProfileRequestModel
    {
        public long MemberId { get; set; }

        public string Title { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Gender { get; set; }

        public string PrimaryContactNumber { get; set; }

        public string SecondaryContactNumber { get; set; }

        public string EmailId { get; set; }

        public string PanCard { get; set; }

        public string AadhaarNo { get; set; }

        public UpdateAddressModel Address { get; set; }
    }

    public class UpdateAddressModel
    {
        public string HouseNo { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string ZipCode { get; set; }
    }

    public class UpdateCityRequest
    {
        public string cityName { get; set; }
        public string pincode { get; set; }
        public bool active { get; set; }
    }
}
