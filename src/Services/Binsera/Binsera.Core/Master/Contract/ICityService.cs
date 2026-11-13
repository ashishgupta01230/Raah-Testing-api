using Binsera.Core.Master.Model;
using HautSphere.BuildingBlocks.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Contract
{
   
        public interface ICityService
        {
            Task<IPagedList<CityModel>> GetAllAsync(CityFilterModel filterModel);
            Task<CityModel?> GetByIdAsync(int id);
            Task UpdateAsync(int id, string CityName, string Pincode, bool active);
        }

    
}
