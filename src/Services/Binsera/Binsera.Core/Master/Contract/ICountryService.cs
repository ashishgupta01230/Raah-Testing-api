using Binsera.Core.Master.Model;
using HautSphere.BuildingBlocks.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Contract
{
   
        public interface ICountryService
        {
            Task<IPagedList<CountryModel>> GetCountries(CountryFilterModel filterModel);

            Task UpdateCountryStatus(int CID, bool cActive);
        }
    
}
