using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class Devices : DeviceBase
    {
        [StringLength(40, ErrorMessage = "More than 40 character is not allowed.")]
        public string? Imei { get; set; }
        public virtual DevicesConfiguration? DevicesConfiguration { get; set; }
        public virtual Locations? Location { get; set; }
        public virtual DeviceTypes? DeviceType { get; set; }

        [NotMapped]
        public int? AdctestsCount { get; set; }

        [NotMapped]
        public int? SenseIterationMinutes { get; set; }

        [NotMapped]
        public int? SendIterationCount { get; set; }

        [NotMapped]
        public int? GPSIterationCount { get; set; }
    }
}
