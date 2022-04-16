using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class FloodEvents : FloodEventBase
    {
        public string? LocationIds { get; set; }
        public Regions? Region { get; set; }
        public virtual ICollection<EventsDetail>? EventsDetail { get; set; }
    }
}
