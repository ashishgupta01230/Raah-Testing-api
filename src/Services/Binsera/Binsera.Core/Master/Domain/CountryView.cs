using HautSphere.BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binsera.Core.Master.Domain
{
    public class CountryView : DomainBase
    {
        public string CountryName { get; private set; }

        public bool cActive { get; private set; }

        private CountryView()
        {

        }

        public void UpdateStatus(bool isActive)
        {
            cActive = isActive;
        }
    }
}
