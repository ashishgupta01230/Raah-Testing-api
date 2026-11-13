using Binsera.Core.Locations.Domain;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Persistence.Extensions
{
    public static class AddViewExtension
    {
        public static void AddViews(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AddressView>().Property(e => e.Id).HasColumnType("int");
            modelBuilder.Entity<AddressView>().ToView(nameof(AddressView).ToLower());
        }
    }
}
