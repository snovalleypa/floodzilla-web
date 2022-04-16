namespace FloodzillaWeb.Models.FzModels
{
    public partial class UserLocations
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public int RegionId { get; set; }

        public Locations Location { get; set; }
        public Regions Regions { get; set; }
        public Users User { get; set; }
    }
}
