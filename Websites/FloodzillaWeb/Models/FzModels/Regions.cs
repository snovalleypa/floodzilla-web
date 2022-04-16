using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class Regions : RegionBase
    {
        public Regions()
        {
            Locations = new HashSet<Locations>();
            FloodEvents = new HashSet<FloodEvents>();
            UserLocations = new HashSet<UserLocations>();
        }

        public int? RecentWeatherId { get; set; }

        public virtual Organizations? Organizations { get; set; }
        public virtual ICollection<Locations>? Locations { get; set; }
        public ICollection<FloodEvents>? FloodEvents { get; set; }

        public ICollection<UserLocations>? UserLocations { get; set; }

    }
}
