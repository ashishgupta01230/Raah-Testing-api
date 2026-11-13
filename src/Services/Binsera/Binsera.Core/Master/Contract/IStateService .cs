using Binsera.Core.Master.Model;
using HautSphere.BuildingBlocks.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Contract
{
    public interface IStateService
    {
        Task<IPagedList<StateModel>> GetStates(StateFilterModel filterModel);
        Task UpdateStateStatus(int id, bool sActive);
    }
}
