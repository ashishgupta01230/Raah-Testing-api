namespace Binsera.Core.Members.Models
{
    public class MemberProfileModel
    {
        public long Id { get; set; }
        public long? RegistrationNumber { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? Gender { get; set; }
        public string PrimaryContactNumber { get; set; } = string.Empty;
        public string SecondaryContactNumber { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public long IntroRegNo { get; set; }
        public char? IntroSide { get; set; }
        public MemberProfileAddressModel Address { get; set; } = new();
        public DateTime? RegistrationDate { get; set; }
        public SponsorModel SponsorDetails { get; set; }
        public string AadhaarNo { get; set; }
        public string MemPan { get; set; }
        public Guid MemberId { get; set; }
        public string ProfileImage { get; set; }
        public int? StatusId { get; set; }
    }

    public class MemberProfileAddressModel
    {
        public string HouseNo { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
    }
    public class MemberLoginDetailsModel
    {
        public long Id { get; set; }

        public string LoginId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}