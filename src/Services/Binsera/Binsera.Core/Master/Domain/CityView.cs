using HautSphere.BuildingBlocks.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Domain
{
 
        public class CityView : DomainBase
        {
            public int CID { get; private set; }
            public int SID { get; private set; }
            public int DistId { get; private set; }
            public string CityName { get; private set; }
            public string Pincode { get; private set; }
            public bool Active { get; private set; }

         private CityView()
        {

        }
        
        public void UpdateStatus(string CityName, string Pincode, bool isActive)
        {
            CityName = CityName;
            Pincode = Pincode;
            Active = isActive;
        }
    }
    
}
