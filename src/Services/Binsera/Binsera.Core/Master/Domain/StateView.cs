using HautSphere.BuildingBlocks.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Domain
{
    public class StateView : DomainBase
    {
        public string StateName { get; private set; }
        public int CID { get; private set; }
        public bool sActive { get; private set; }

        private StateView()
        {
        }

        public void UpdateStatus(bool isActive)
        {
            sActive = isActive;
        }
    }

}
