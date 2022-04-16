namespace FloodzillaWeb.Models.FzModels
{
    public partial class Weathers
    {
        public int WeatherId { get; set; }
        public int? RegionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string WeatherStatus { get; set; }
        public double? Temperature { get; set; }
        public double? Precip1HourMm { get; set; }
        public string ResponseString { get; set; }
    }
}
