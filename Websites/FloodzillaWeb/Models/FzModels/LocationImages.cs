namespace FloodzillaWeb.Models.FzModels
{
    public partial class LocationImages
    {
        public int LocationId { get; set; }
        public string Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public string Image4 { get; set; }

        public virtual Locations Location { get; set; }
    }
}
