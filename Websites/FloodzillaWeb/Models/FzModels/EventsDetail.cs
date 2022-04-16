namespace FloodzillaWeb.Models.FzModels
{
    public class EventsDetail
    {
        public int EventId { get; set; }
        public int LocationId { get; set; }
        public virtual Locations Location { get; set; }
        public virtual FloodEvents Floodevent { get; set; }
    }
}
