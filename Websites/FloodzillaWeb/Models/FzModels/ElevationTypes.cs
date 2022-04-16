namespace FloodzillaWeb.Models.FzModels
{
    public partial class ElevationTypes
    {
        public ElevationTypes()
        {
            Elevations = new HashSet<Elevations>();
        }

        public int ElevationTypeId { get; set; }
        public string ElevationTypeName { get; set; }

        public virtual ICollection<Elevations> Elevations { get; set; }
    }
}
