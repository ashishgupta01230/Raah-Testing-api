using Binsera.Core.Members.Contracts;
using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace Binsera.Application.Members.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class KitMasterService(IUnitOfWork unitOfWork) : IKitMasterService
    {
        private readonly IRepository<KitMaster> _kitMasterRepository =
            unitOfWork.Repository<KitMaster>();

        public async Task<KitItemModel> GetKit(long id)
        {
            return await _kitMasterRepository.TableNoTracking
                .Where(x => x.Id == id)
                .Select(x => new KitItemModel
                {
                    Id = x.Id,
                    KitCode = x.KitCode,
                    KitPrice = x.KitPrice,
                    KitDesc = x.KitDesc,
                    KitProducts = x.KitProducts,
                })
                .FirstAsync();
        }

        public async Task<List<KitItemModel>> GetKits()
        {
            return await _kitMasterRepository.TableNoTracking.Where(s => s.KitActive == true && s.Id > 1)
                .Select(x => new KitItemModel
                {
                    Id = x.Id,
                    KitCode = x.KitCode,
                    KitPrice = x.KitPrice,
                    KitDesc = x.KitDesc,
                    KitProducts = x.KitProducts,


                })
                .ToListAsync();
        }
    }
}