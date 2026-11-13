#nullable disable
namespace Binsera.Core.Members.Models
{
    public class MemberRegistrationModel
    {
        //public int BussinessCategoryId { get; set; }
        public long IntroRegNo { get; set; }
        public PersonInfoModel PersonInfo { get; set; }
        // public AdditionalInfoModel AdditionalInfo { get; set; }
        public AddressEditModel Address { get; set; }
        public char? IntroSide { get; set; }
    }
    public class PersonInfoModel
    {
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        //public DateTime? DateOfBirth { get; }
        public int Gender { get; set; }
        public string PrimaryContactNumber { get; set; }
        public string SecondaryContactNumber { get; }
        public string EmailId { get; set; }
        public AddressEditModel Address { get; set; }
        public string UserName { get; set; }
        public string AadhaarNo { get; set; }
        public string PanCard { get; set; }
    }
    public class AdditionalInfoModel
    {
        public int[] CourseIds { get; set; }
        public string WhyThisCourse { get; set; }
        public string CurrentInstitute { get; set; }
        public string AppliedFor { get; set; }
        public string RefrenceSources { get; set; }
    }
}
