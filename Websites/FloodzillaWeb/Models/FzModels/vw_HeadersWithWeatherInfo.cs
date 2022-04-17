namespace FloodzillaWeb.Models.FzModels
{
    public class vw_HeadersWithWeatherInfo
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int DeviceId { get; set; }
        public int? TimeBetweenADC { get; set; }
        public int? StartTempTop { get; set; }
        public int? StartTempBottom { get; set; }
        public int? Version { get; set; }
        public int? Count { get; set; }
        public int? BatteryVolt { get; set; }
        public int? HeaterVolt { get; set; }
        public int? HeaterOnBatteryVolt { get; set; }
        public int? HeaterOnTime { get; set; }
        public int? WeatherId { get; set; }
        public string Note { get; set; }
        public string AggNote { get; set; }

        public int? LocationId { get; set; }
        public double? Temperature { get; set; }
        public double? Precip1HourMM { get; set; }
    }
}
