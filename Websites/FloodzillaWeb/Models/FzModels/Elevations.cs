using System.ComponentModel.DataAnnotations.Schema;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class Elevations
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ElevationId { get; set; }
        public string ElevationName { get; set; }
        public int ElevationTypeId { get; set; }
        public int LocationId { get; set; }
        public double Elevation { get; set; }

        public virtual ElevationTypes ElevationType { get; set; }
        public virtual Locations Location { get; set; }
    }
}
