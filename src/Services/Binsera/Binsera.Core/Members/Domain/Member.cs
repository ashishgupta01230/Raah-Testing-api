using Binsera.Core.Locations.Models;
using Binsera.Core.Media.Events;
using Binsera.Core.Members.Events;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

#nullable disable
namespace Binsera.Core.Members.Domain
{
    public class Member : DomainBase
    {
        private Member()
        {

        }
        private Member(MemberRegistrationModel registration)
        {
            MemberGraphId = Guid.NewGuid();
            //Personal Details
            Title = registration.PersonInfo.Title;
            FName = registration.PersonInfo.FirstName;
            LName = registration.PersonInfo.LastName;
            //Dob = registration.PersonInfo.DateOfBirth;
            Gender = registration.PersonInfo.Gender;

            //Contact Details
            Mobile = registration.PersonInfo.PrimaryContactNumber;
            Phone = registration.PersonInfo.SecondaryContactNumber;
            EmailID = registration.PersonInfo.EmailId;

            //Id Proof Details
            AadhaarNo = registration.PersonInfo.AadhaarNo;
            MemPan = registration.PersonInfo.PanCard;

            //Address details
            Address = registration.Address.HouseNo;
            Address2 = registration.Address.Street;
            District = registration.Address.DistId?.ToString();
            OCity = registration.Address.City;
            OState = registration.Address.State;
            Sid = registration.Address.StateId;
            Ctid = registration.Address.CityId;
            PinCode = registration.Address.ZipCode;
            Cid = registration.Address.CountryId;

            //Tree Details
            // A member who signs up without naming a sponsor falls back to the company's own
            // account. That used to be RegNo 3 ("Companyid"); RegNo 3 is now the X3 seed account,
            // so the fallback points at X1 -- the bottom of the seed chain, and the account the
            // first real member sits under. Sponsorship is only a record here; it has no bearing
            // on where the member is placed in the pool.
            IntroRegNo = registration.IntroRegNo == 0 ? 1 : registration.IntroRegNo;
            IntroSide = registration.IntroSide; // Default Left side
            PayMode = "Free";
            RegDate = DateTime.Now;
            //BussinessCategoryId = registration.BussinessCategoryId;
            UpdateBy = "Self";
            UpdateDate = DateTime.Now;
            PayMode = "Free";
            PaymentMode = "Free";
            DlPayId = 0;
            DirectMembers = 0;
            ParentRegNoB = 0;
            DepthsM = 0;
            DepthsB = 0;
            ParentPosB = 0;
            Active = true;
            ParentRegNoM = 0;
            PPositionM = 0;
            ParentPosM = 0;
            Slid = 0;
            AddDomainEvent(new MemberCreatedUserDomainEvent(this));
        }
        public Guid MemberGraphId { get; private set; }
        public long? RegNo { get; private set; }
        public long? MemCode { get; private set; }
        public string LoginID { get; set; }
        public string Title { get; private set; }
        public string FName { get; private set; }
        public string LName { get; private set; }
        public DateTime? Dob { get; private set; }
        public int? Gender { get; private set; }
        public long? DirectMembers { get; private set; }
        //-----------------------  Accounts ---------------------
        public string AccountNo { get; private set; }
        public string NameOnAccount  { get; private set; }   
        public string accountType { get; private set; } 
        public string BankName { get; private set; }
        public string IFSCCode { get; private set; }    
        public string branchName { get; private set; }  
       public string BranchCode { get; private set; }
        public string Address { get; private set; }
        public string Address2 { get; private set; }
        public string LandMark { get; private set; }
        public int? Cid { get; private set; }
        public int? Sid { get; private set; }
        public string OState { get; private set; }
        public int? Ctid { get; private set; }
        public string PinCode { get; private set; }
        public string OCity { get; private set; }
        public string District { get; private set; }
        [Column("BrID")]
        public int? BussinessCategoryId { get; private set; }
        public string MemPan { get; private set; }
        public string Phone { get; private set; }
        public string Mobile { get; private set; }
        public string EmailID { get; private set; }
        public int KID { get; private set; }
        public decimal? KitPrice { get; private set; }
        public double? KitBV { get; private set; }
        public double? KitPV { get; private set; }
        public DateTime? RegDate { get; private set; }
        public int? RegPayNo { get; private set; }
        public int? ConfPayNo { get; private set; }
        public string FirstPassword { get; private set; }
        public string SecurityCode { get; private set; }
        public long IntroRegNo { get; private set; }
        public char? IntroSide { get; private set; }
        public int? IntroPosM { get; private set; }
        public int? DepthsM { get; private set; }
        public long? ParentRegNoB { get; private set; }
        public char? ParentSideB { get; private set; }
        public int? ParentPosB { get; private set; }
        public bool? Active { get; private set; }
        public int? DepthsB { get; private set; }
        public string JoinAs { get; private set; }
        public string CommentOnActive { get; private set; }
        public string UpdateBy { get; private set; }
        public DateTime UpdateDate { get; private set; }
        public int BLevel { get; private set; }
        public int LDirect { get; private set; }
        public int RDirect { get; private set; }
        public string PayMode { get; private set; }
        public string PaymentMode { get; private set; }
        public int DlPayId { get; private set; }
        public string AadhaarNo { get; private set; }
        public long? ParentRegNoM { get; private set; }
        public long? PPositionM { get; private set; }
        public long? ParentPosM { get; private set; }
        public int? Slid { get; private set; }
        public string ProfileImage { get; private set; }

        public MemberAdditionalInfo MemberAdditionalInfo { get; private set; }

        [NotMapped]
        public override bool IsDeleted { get; set; }
        public static Member CreateDomain(MemberRegistrationModel model)
        {
            return new Member(model);
        }

        public void SetKitMaster(KitItemModel kitMaster)
        {
            KitBV = (double)kitMaster.KitBV;
            KitPV = (double)kitMaster.KitPV;
            KitPrice = kitMaster.KitPrice;
            KID = (int)kitMaster.Id;
        }
        public void UpdatePayNo(int confPayNo)
        {
            ConfPayNo = confPayNo;
            AddDomainEvent(new MemberPayNoUpdatedDomainEvent(this.Id, this.MemberGraphId, ConfPayNo));
        }

        /// <summary>
        /// Records where this member sits in the Global Auto Pool tree. These are the legacy binary
        /// placement columns, which a SQL trigger used to write before the pool took the job over;
        /// PPositionM carries the member's own level-order position, from which parent, side and
        /// depth all follow.
        /// </summary>
        public void PlaceInPool(long position, long? parentRegNo, char? side, int depth)
        {
            PPositionM = position;
            ParentRegNoB = parentRegNo ?? 0;
            ParentSideB = side;
            ParentPosB = side == 'L' ? 1 : side == 'R' ? 2 : 0;
            DepthsB = depth;
        }

        public bool IsPlacedInPool => PPositionM.HasValue && PPositionM.Value > 0;
        public void UpdateProfileImage(long profileImageId)
        {
            ProfileImage = profileImageId.ToString();
            AddDomainEvent(new ImageUploadedConfirmationDomainEvent(profileImageId, "ProfileImage", this.Id));
            //AddDomainEvent(new MemberProfileImageUpdatedDomainEvent(this.Id, this.MemberGraphId, ProfileImage));
        }
        public void UpdateProfile(PersonInfoModel model)
        {
            Title = model.Title ?? "";
            FName = model.FirstName ?? "";
            LName = model.LastName ?? "";
            Gender = model.Gender;
            Mobile = model.PrimaryContactNumber ?? "";
            Phone = model.SecondaryContactNumber ?? "";
            EmailID = model.EmailId ?? "";
            AadhaarNo = model.AadhaarNo ?? "";
            MemPan = model.PanCard ?? "";

            Address = model.Address.HouseNo;
            Address2 = model.Address.Street;
            District = model.Address.DistId?.ToString();
            OCity = model.Address.City;
            OState = model.Address.State;
            Sid = model.Address.StateId;
            Ctid = model.Address.CityId;
            PinCode = model.Address.ZipCode;
            Cid = model.Address.CountryId;

            AddDomainEvent(new MemberProfileUpdatedDomainEvent(
                this.Id, this.MemberGraphId,
                Title, FName, LName, EmailID, Mobile, OState, PinCode, ProfileImage ?? ""));
        }

        // An IFSC is always 4 letters (bank), a literal 0, then 6 alphanumeric branch characters --
        // SBIN0000001. Validating the shape here rather than in the controller keeps the rule true
        // for every caller, and the value is upper-cased and stripped of the spaces people type.
        private static readonly Regex IfscPattern = new(@"^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.Compiled);

        private static readonly string[] AllowedAccountTypes = { "Savings", "Current" };

        /// <summary>
        /// Records the bank account any payout to this member would be made into. Validated on the
        /// entity so a malformed account can never be persisted, no matter which caller asks.
        /// </summary>
        public void UpdateBankDetails(MemberBankAccountModel model)
        {
            if (model == null)
                throw new InvalidOperationException("Bank details are required.");

            var accountNo = (model.AccountNo ?? "").Replace(" ", "").Replace("-", "");
            var ifsc = (model.IFSCCode ?? "").Replace(" ", "").ToUpperInvariant();
            var accountType = (model.AccountType ?? "").Trim();

            if (string.IsNullOrWhiteSpace(model.BankName))
                throw new InvalidOperationException("Bank name is required.");

            if (string.IsNullOrWhiteSpace(model.NameOnAccount))
                throw new InvalidOperationException("Name on account is required.");

            if (accountNo.Length is < 6 or > 18 || !accountNo.All(char.IsDigit))
                throw new InvalidOperationException("Account number must be 6 to 18 digits.");

            if (!IfscPattern.IsMatch(ifsc))
                throw new InvalidOperationException("IFSC code must be 11 characters, for example SBIN0000001.");

            if (accountType.Length > 0 && Array.IndexOf(AllowedAccountTypes, accountType) < 0)
                throw new InvalidOperationException("Account type must be Savings or Current.");

            AccountNo = accountNo;
            NameOnAccount = model.NameOnAccount.Trim();
            BankName = model.BankName.Trim();
            IFSCCode = ifsc;
            branchName = (model.BranchName ?? "").Trim();

            // The local shadows the property of the same name, so this one needs an explicit this.
            this.accountType = accountType;

            AddDomainEvent(new MemberBankAccountUpdatedDomainEvent(
                this.Id, this.MemberGraphId, NameOnAccount, BankName, IFSCCode, AccountNo));
        }
    }
}