using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class DevicesConfiguration
    {
        [Required(ErrorMessage = "Device is required.")]
        public int DeviceId { get; set; }
        [Required(ErrorMessage = "Minutes b/w Batches is required.")]
        public int MinutesBetweenBatches { get; set; }
        [Required(ErrorMessage = "Seconds b/w ADCSense is required.")]
        public int SecBetweenAdcsense { get; set; }
        public int? AdctestsCount { get; set; }
        public int? SenseIterationMinutes { get; set; }
        public int? SendIterationCount { get; set; }
        public int? GPSIterationCount { get; set; }
        public virtual Devices Device { get; set; }
    }
}
