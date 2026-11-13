using Binsera.Core;
using Binsera.Core.Locations.Contracts;
using Binsera.Core.Members.Contracts;
using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace Binsera.Application.Members.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class MemberRegistrationService(
        IUnitOfWork unitOfWork,
        IKitMasterService kitMasterService,
        IAddressViewService addressViewService,
        ILogger logger) : IMemberRegistrationService
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        public async Task<MemberProfileModel> GetMemberProfileAsync(string userName)
        {
            return await GetMemberProfileByLoginIdAsync(userName) ?? throw new Exception("Member not found");
        }
        public async Task<MemberProfileModel?> GetMemberProfileByLoginIdAsync(string loginId)
        {
            var member = await _memberRepository.TableNoTracking
                .Where(x => x.LoginID == loginId)
                .FirstOrDefaultAsync();

            if (member == null)

                return null;

            return await GetMemberProfileByGraphIdAsync(member.MemberGraphId);
        }

        public async Task<MemberProfileModel?> GetMemberProfileByGraphIdAsync(Guid memberGraphId)
        {
            var memberProfile = await _memberRepository.TableNoTracking
                .Where(x => x.MemberGraphId == memberGraphId)
                .Select(member => new
                {
                    Id = member.Id,
                    RegistrationNumber = member.RegNo,
                    LoginId = member.LoginID,
                    Title = member.Title,
                    FirstName = member.FName,
                    LastName = member.LName,
                    Gender = member.Gender,
                    PrimaryContactNumber = member.Mobile,
                    SecondaryContactNumber = member.Phone,
                    EmailId = member.EmailID,
                    IntroRegNo = member.IntroRegNo,
                    IntroSide = member.IntroSide,
                    HouseNo = member.Address,
                    Street = member.Address2,
                    City = member.OCity,
                    State = member.OState,
                    ZipCode = member.PinCode,
                    CountryId = member.Cid,
                    StateId = member.Sid,
                    CityId = member.Ctid,
                    District = member.District,
                    RegistrationDate = member.RegDate,
                    AadhaarNo = member.AadhaarNo,
                    PanCard = member.MemPan,
                    member.MemberGraphId,
                    member.ProfileImage,
                    member.ConfPayNo,
                })
                .FirstOrDefaultAsync();

            if (memberProfile == null)
                return null;

            int? districtId = int.TryParse(memberProfile.District, out var parsedDistrictId)
                ? parsedDistrictId
                : null;

            var memberDetails = new MemberProfileModel
            {
                Id = memberProfile.Id,
                MemberId = memberProfile.MemberGraphId,
                RegistrationNumber = memberProfile.RegistrationNumber,
                LoginId = memberProfile.LoginId,
                Title = memberProfile.Title,
                FirstName = memberProfile.FirstName,
                LastName = memberProfile.LastName,
                Gender = memberProfile.Gender,
                PrimaryContactNumber = memberProfile.PrimaryContactNumber,
                SecondaryContactNumber = memberProfile.SecondaryContactNumber,
                EmailId = memberProfile.EmailId,
                IntroRegNo = memberProfile.IntroRegNo,
                IntroSide = memberProfile.IntroSide,
                RegistrationDate = memberProfile.RegistrationDate,
                AadhaarNo = memberProfile.AadhaarNo,
                MemPan = memberProfile.PanCard,
                ProfileImage = memberProfile.ProfileImage,
                StatusId = memberProfile.ConfPayNo,
                Address = new MemberProfileAddressModel
                {
                    HouseNo = memberProfile.HouseNo,
                    Street = memberProfile.Street,
                    City = memberProfile.City,
                    State = memberProfile.State,
                    ZipCode = memberProfile.ZipCode,
                    CountryId = memberProfile.CountryId,
                    StateId = memberProfile.StateId,
                    CityId = memberProfile.CityId,
                    DistrictId = districtId
                }
            };
            memberDetails.SponsorDetails = await GetSponsorByIntroId(memberProfile.IntroRegNo);
            return memberDetails;
        }
        public async Task<SponsorModel> GetSponsorByIntroId(long introRegNo)
        {
            var sponsor = await unitOfWork.Repository<Member>().TableNoTracking
                        .Where(x => x.RegNo == introRegNo)
                        .Select(f => new SponsorModel
                        {
                            IntoRegNo = f.RegNo,
                            Title = f.Title,
                            FName = f.FName,
                            LName = f.LName,
                            LoginId = f.LoginID,
                            PrimaryContactNumber = f.Mobile,
                            MemberId = f.MemberGraphId,
                            ProfileImage = f.ProfileImage,
                        })
                        .FirstOrDefaultAsync();
            return sponsor ?? default!;
        }

        public async Task<SponsorModel> GetSponsor(string sponsorId)
    {
            var sponsor = await unitOfWork.Repository<Member>().TableNoTracking
                        .Where(x => x.LoginID == sponsorId.ToUpper() && x.ConfPayNo == 1)
                        .Select(f => new SponsorModel
                        {
                            IntoRegNo = f.RegNo,
                            Title = f.Title,
                            FName = f.FName,
                            LName = f.LName
                        })
                        .FirstOrDefaultAsync();
            return sponsor ?? default!;
        }
        public async Task<Member> RegisterMemberAsync(MemberRegistrationModel memberRegistration)
        {
            logger.Information("Registering Member Payload: {memberRegistration}", JsonConvert.SerializeObject(memberRegistration));
            //Get Default Kit
            var kitMaster = await kitMasterService.GetKit(CompanySetting.Default.DefaultKitId);
            //set Address Id Details
            var address = await addressViewService.GetAddressesByPinCode(memberRegistration.Address.ZipCode);
            if (address == null || !address.Any())
                address = await addressViewService.GetAddressesByCityName(memberRegistration.Address.City);
            if (address != null && address.Any())
                memberRegistration.Address.SetAddressModel(address);
            logger.Information("Registering Member modified Payload: {memberRegistration}", JsonConvert.SerializeObject(memberRegistration));
            var domain = Member.CreateDomain(memberRegistration);  
            domain.SetKitMaster(kitMaster);
            await _memberRepository.Save(domain);
            return domain;
        }
        public async Task<bool> UpdateMemberProfileAsync(string userName, PersonInfoModel model)
        {
            var member = await _memberRepository.Table
                .FirstOrDefaultAsync(x => x.LoginID == userName);

            if (member == null)
                return false;
            var address = await addressViewService.GetAddressesByPinCode(model.Address.ZipCode);
            if (address == null || !address.Any())
                address = await addressViewService.GetAddressesByCityName(model.Address.City);
            if (address != null && address.Any())
                model.Address.SetAddressModel(address);

            member.UpdateProfile(model);
            await _memberRepository.Save(member);
            return true;
        }
        public async Task<bool> UpdateMemberProfileImageAsync(string userName, long profileId)
        {
            var member = await _memberRepository.Table
                .FirstOrDefaultAsync(x => x.LoginID == userName);
            if (member == null)
                return false;
            member.UpdateProfileImage(profileId);
            await _memberRepository.Save(member);
            return true;
        }
    }
}
