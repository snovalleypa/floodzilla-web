using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

using FzCommon;

namespace FloodzillaWeb.Controllers.Api
{
    public class ApiV2Region
    {
        public const string DEFAULT_TIMEZONE = "America/Los_Angeles";
        public const string DEFAULT_URL = "https://floodzilla.com";

        public int Id { get; }
        public string Name { get; }
        public string Timezone { get; }
        public string BaseUrl { get; }
        // Deprecated
        public string[] DefaultForecastGageList { get; }
        public string[] DefaultForecastGaugeList { get; }
        public ApiV2Region(RegionBase source)
        {
            this.Id = source.RegionId;
            this.Name = source.RegionName;
            this.Timezone = source.IanaTimeZone ?? DEFAULT_TIMEZONE;
            this.BaseUrl = source.BaseURL ?? DEFAULT_URL;
            this.DefaultForecastGageList = (source.DefaultForecastGageList ?? "").Split(",");
            this.DefaultForecastGaugeList = (source.DefaultForecastGageList ?? "").Split(",");
        }
    }

    public class ApiV2Metagauge
    {
        public string Ids { get; }
        public string SiteIds { get; }
        public string Name { get; }
        public string ShortName { get; }
        public double StageOne { get; }
        public double StageTwo { get; }
        public ApiV2Metagauge(string ids, string siteIds, string name, string shortName, double stageOne, double stageTwo)
        {
            Ids = ids;
            SiteIds = siteIds;
            Name = name;
            ShortName = shortName;
            StageOne = stageOne;
            StageTwo = stageTwo;
        }
    }

    // This class is for the parallel-arrays version of the response.  It produces a JSON
    // blob about half the size of the array-of-objects version.
    public class ApiV2ForecastReadingSet
    {
        public DateTime[] Timestamps { get; }
        public double?[]? WaterHeights { get; }
        public double?[] Discharges { get; }
        public ApiV2ForecastReadingSet(NoaaForecastItem[] data, bool isMetagauge)
        {
            this.Timestamps = new DateTime[data.Length];
            if (!isMetagauge)
            {
                this.WaterHeights = new double?[data.Length];
            }
            this.Discharges = new double?[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                this.Timestamps[i] = new DateTime(data[i].Timestamp.Ticks, DateTimeKind.Utc);
                if (!isMetagauge && (this.WaterHeights != null))
                {
                    this.WaterHeights[i] = data[i].Stage;
                }
                this.Discharges[i] = data[i].Discharge;
            }
        }
    }
    public class ApiV2Forecast : ApiV2ForecastReadingSet
    {
        public string NoaaSiteId { get; }
        public DateTime ForecastCreated { get; }
        public int ForecastId { get; }
        public ApiV2ForecastReadingSet Peaks { get; }
        public ApiV2Forecast(NoaaForecast noaaForecast, bool isMetagauge) : base(noaaForecast.Data.ToArray(), isMetagauge)
        {
            this.NoaaSiteId = noaaForecast.NoaaSiteId;
            this.ForecastId = noaaForecast.ForecastId;
            this.ForecastCreated = new DateTime(noaaForecast.Created.Ticks, DateTimeKind.Utc);
            this.Peaks = new ApiV2ForecastReadingSet(noaaForecast.Peaks, isMetagauge);
        }
    }
    public class ApiV2ForecastResponse : Dictionary<string, ApiV2Forecast?>
    {
    }

    // These versions of the above classes are for the array-of-objects format.  If it proves
    // easier to consume this on the client, we can switch back
    public class ApiV2Forecast_DataPoint
    {
        public DateTime Timestamp;
        public double? WaterHeight;
        public double? Discharge;
    }
    public class ApiV2ForecastExp
    {
        public string NoaaSiteId { get; }
        public DateTime ForecastCreated { get; }
        public int ForecastId { get; }
        public ApiV2Forecast_DataPoint[] Data;
        public ApiV2Forecast_DataPoint[] Peaks;
        public ApiV2ForecastExp(NoaaForecast noaaForecast, bool isMetagauge)
        {
            this.NoaaSiteId = noaaForecast.NoaaSiteId;
            this.ForecastId = noaaForecast.ForecastId;
            this.ForecastCreated = new DateTime(noaaForecast.Created.Ticks, DateTimeKind.Utc);
            this.Data = this.ConvertData(noaaForecast.Data.ToArray(), isMetagauge);
            this.Peaks = this.ConvertData(noaaForecast.Peaks, isMetagauge);
        }
        private ApiV2Forecast_DataPoint[] ConvertData(NoaaForecastItem[] data, bool isMetagauge)
        {
            ApiV2Forecast_DataPoint[] ret = new ApiV2Forecast_DataPoint[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                ret[i] = new ApiV2Forecast_DataPoint()
                {
                    Timestamp = new DateTime(data[i].Timestamp.Ticks, DateTimeKind.Utc),
                    Discharge = data[i].Discharge,
                };
                if (!isMetagauge)
                {
                    ret[i].WaterHeight = data[i].Stage;
                }
            }
            return ret;
        }
    }

    public class ApiV2ForecastResponseExp : Dictionary<string, ApiV2ForecastExp?>
    {
    }

    public class ApiV2ReadingSet
    {
        public double? TrendCfsPerHour { get; }
        public double? TrendFeetPerHour { get; }
        public int[]? ReadingIds { get; }
        public DateTime[] Timestamps { get; }
        public double?[]? WaterHeights { get; }
        public double?[] Discharges { get; }
        public ApiV2ReadingSet(List<SensorReading> readings, int prevMaxId)
        {
            bool hasIds = (readings[0].Id != 0);
            bool hasWaterHeight = (readings[0].WaterHeightFeet.HasValue);
            if (readings.Count >= Trends.DesiredReadingCount)
            {
                Trends dischargeTrends = TrendCalculator.CalculateDischargeTrends(readings);
                this.TrendCfsPerHour = dischargeTrends.TrendValue;
                if (hasWaterHeight)
                {
                    Trends waterTrends = TrendCalculator.CalculateWaterTrends(readings);
                    this.TrendFeetPerHour = waterTrends.TrendValue;
                }
            }
            int readingCount;
            if (prevMaxId > 0 && hasIds)
            {
                readingCount = 0;
                foreach (SensorReading reading in readings)
                {
                    if (reading.Id > prevMaxId)
                    {
                        readingCount++;
                    }
                }
            }
            else
            {
                readingCount = readings.Count;
            }
            if (hasIds)
            {
                this.ReadingIds = new int[readingCount];
            }
            this.Timestamps = new DateTime[readingCount];
            this.Discharges = new double?[readingCount];
            if (hasWaterHeight)
            {
                this.WaterHeights = new double?[readingCount];
            }
            for (int i = 0; i < readingCount; i++)
            {
                SensorReading reading = readings[i];
                if (hasIds && (this.ReadingIds != null))
                {
                    this.ReadingIds[i] = reading.Id;
                }
                this.Timestamps[i] = new DateTime(reading.Timestamp.Ticks, DateTimeKind.Utc);
                this.Discharges[i] = reading.WaterDischarge;
                if (hasWaterHeight && (this.WaterHeights != null))
                {
                    this.WaterHeights[i] = reading.WaterHeightFeet;
                }
            }
        }
    }
    public class ApiV2ReadingResponse
    {
        public Dictionary<string, ApiV2ReadingSet> Readings { get; set; }
        public int MaxReadingId                             { get; set; }
        public ApiV2ReadingResponse()
        {
            this.Readings = new();
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [AllowAnonymous]
    [Route("api/v2/[action]")]
    [JwtHeaderFilter]
    public class ApiV2Controller : Controller
    {
        public async Task<IActionResult> GetRegion(int regionId)
        {
            using SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
            sqlcn.Open();

            RegionBase region = await RegionBase.GetRegionAsync(sqlcn, regionId);
            if (region == null)
            {
                return NotFound(String.Format("Region '{0}' not found", regionId));
            }
            ApiV2Region result = new(region);

            // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
            // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
            // quoting the way OkObjectResult does.
            return new ContentResult() { Content = JsonConvert.SerializeObject(result), ContentType = "application/json" };
        }

        // Deprecated due to "gage" spelling
        public async Task<IActionResult> GetDefaultForecastGageList(int regionId)
        {
            return await this.GetDefaultForecastGaugeList(regionId);
        }

        public async Task<IActionResult> GetDefaultForecastGaugeList(int regionId)
        {
            using SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
            sqlcn.Open();

            RegionBase region = await RegionBase.GetRegionAsync(sqlcn, regionId);
            if (region == null)
            {
                return NotFound(String.Format("Region '{0}' not found", regionId));
            }
            string? gageList = region.DefaultForecastGageList;
            string[] result = new string[0];
            if (!String.IsNullOrEmpty(gageList))
            {
                result = gageList.Split(",");
            }

            // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
            // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
            // quoting the way OkObjectResult does.
            return new ContentResult() { Content = JsonConvert.SerializeObject(result), ContentType = "application/json" };
        }

        // Deprecated due to "gage" spelling
        public IActionResult GetMetagages(int regionId)
        {
            return this.GetMetagauges(regionId);
        }

        public IActionResult GetMetagauges(int regionId)
        {
            List<ApiV2Metagauge> result = new();
            if (regionId == Metagages.MetagageRegion)
            {
                for (int i = 0; i < Metagages.MetagageIds.Length; i++)
                {
                    result.Add(new ApiV2Metagauge(Metagages.MetagageIds[i],
                                              Metagages.MetagageSiteIds[i],
                                              Metagages.MetagageNames[i],
                                              Metagages.MetagageShortNames[i],
                                              Metagages.MetagageStageOnes[i],
                                              Metagages.MetagageStageTwos[i]));
                }
            }

            // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
            // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
            // quoting the way OkObjectResult does.
            return new ContentResult() { Content = JsonConvert.SerializeObject(result), ContentType = "application/json" };
        }

        private async Task<NoaaForecast?> GetLatestForecastForGauge(SqlConnection sqlcn, string gauge, List<SensorLocationBase> locations, List<DeviceBase> devices, List<UsgsSite> usgsSites)
        {
            SensorLocationBase? location = locations.Find(l => l.PublicLocationId == gauge);
            if (location == null)
            {
                return null;
            }
            DeviceBase? device = devices.Find(d => d.LocationId == location.Id);
            if (device == null)
            {
                return null;
            }
            if (device.DeviceTypeId != DeviceTypeIds.Usgs || device.UsgsSiteId == 0)
            {
                return null;
            }
            UsgsSite? site = usgsSites.Find(s => s.SiteId == device.UsgsSiteId);
            if (site == null)
            {
                return null;
            }

            return await NoaaForecast.GetLatestSavedForecast(sqlcn, site.NoaaSiteId);
        }

        public async Task<IActionResult> GetForecast(int regionId, string gaugeIds, DateTime? ifNewerThan)
        {
            ApiV2ForecastResponse result = new();
            using SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
            sqlcn.Open();

            // Fetch all locations/devices/USGS sites because we need them to look up NoaaSiteId
            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsForRegionAsync(sqlcn, regionId);
            List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
            List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);

            foreach (string gauge in gaugeIds.Split(','))
            {
                if (gauge.Contains('/'))
                {
                    List<NoaaForecast> forecasts = new();
                    bool anyNew = false;
                    foreach (string subGauge in gauge.Split('/'))
                    {
                        NoaaForecast? subForecast = await this.GetLatestForecastForGauge(sqlcn, subGauge, locations, devices, usgsSites);
                        if (subForecast == null)
                        {
                            return NotFound(String.Format("Gauge '{0}' not found", subGauge));
                        }
                        if (!ifNewerThan.HasValue || (subForecast.Created.Ticks > ifNewerThan.Value.Ticks))
                        {
                            anyNew = true;
                        }
                        forecasts.Add(subForecast);
                    }
                    if (!anyNew)
                    {
                        result[gauge] = null;
                    }
                    else
                    {
                        result[gauge] = new ApiV2Forecast(NoaaForecast.SumForecasts(forecasts), true);
                    }
                }
                else
                {
                    NoaaForecast? forecast = await this.GetLatestForecastForGauge(sqlcn, gauge, locations, devices, usgsSites);
                    if (forecast == null)
                    {
                        return NotFound(String.Format("Gauge '{0}' not found", gauge));
                    }
                    // This compares Ticks because of DateTimeKind issues -- forecast.Created is DateTimeKind.Unspecified
                    if (ifNewerThan.HasValue && (forecast.Created.Ticks <= ifNewerThan.Value.Ticks))
                    {
                        result[gauge] = null;
                    }
                    else
                    {
                        result[gauge] = new ApiV2Forecast(forecast, false);
                    }
                }
            }

            // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
            // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
            // quoting the way OkObjectResult does.
            return new ContentResult() { Content = JsonConvert.SerializeObject(result), ContentType = "application/json" };
        }

        public async Task<IActionResult> GetRecentReadings(int regionId, string gaugeIds, int minutes, int prevMaxReadingId)
        {
            ApiV2ReadingResponse result = new();
            using SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
            sqlcn.Open();

            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsForRegionAsync(sqlcn, regionId);
            List<int> locationIds = new();

            DateTime toDate = DateTime.UtcNow;
            DateTime fromDate = toDate.AddMinutes(-minutes);
            
            foreach (string gauge in gaugeIds.Split(','))
            {
                if (gauge.Contains('/'))
                {
                    foreach (string subGauge in gauge.Split('/'))
                    {
                        SensorLocationBase? location = locations.Find(l => l.PublicLocationId == subGauge);
                        if (location == null)
                        {
                            return NotFound(String.Format("Gauge '{0}' not found", subGauge));
                        }
                        locationIds.Add(location.Id);
                    }
                }
                else
                {
                    SensorLocationBase? location = locations.Find(l => l.PublicLocationId == gauge);
                    if (location == null)
                    {
                        return NotFound(String.Format("Gauge '{0}' not found", gauge));
                    }
                    locationIds.Add(location.Id);
                }
            }
            Dictionary<int, List<SensorReading>> allReadings
                    = await SensorReading.GetMinimalReadingsForLocations(sqlcn,
                                                                         locationIds,
                                                                         fromDate,
                                                                         toDate);
            result.MaxReadingId = prevMaxReadingId;
            foreach (string gauge in gaugeIds.Split(','))
            {
                if (gauge.Contains('/'))
                {
                    List<Queue<SensorReading>> subReadings = new();
                    foreach (string subGauge in gauge.Split('/'))
                    {
                        SensorLocationBase? location = locations.Find(l => l.PublicLocationId == subGauge);
                        if (location == null || !allReadings.ContainsKey(location.Id))
                        {
                            return NotFound(String.Format("Gauge '{0}' not found", subGauge));
                        }
                        Queue<SensorReading> queue;
                        List<SensorReading> readings = allReadings[location.Id];
                        if (readings[0].Id > result.MaxReadingId)
                        {
                            result.MaxReadingId = readings[0].Id;
                        }
                        if (prevMaxReadingId == 0)
                        {
                            queue = new Queue<SensorReading>(readings);
                        }
                        else
                        {
                            queue = new Queue<SensorReading>();
                            foreach (SensorReading reading in readings)
                            {
                                if (reading.Id < prevMaxReadingId)
                                {
                                    break;
                                }
                                queue.Enqueue(reading);
                            }
                        }
                        subReadings.Add(queue);
                    }
                    List<SensorReading> summed = MetagageHelpers.SumReadings(subReadings);
                    if (summed.Count > 0)
                    {
                        result.Readings.Add(gauge, new ApiV2ReadingSet(summed, prevMaxReadingId));
                    }
                }
                else
                {
                    SensorLocationBase? location = locations.Find(l => l.PublicLocationId == gauge);
                    if (location == null || !allReadings.ContainsKey(location.Id))
                    {
                        return NotFound(String.Format("Gauge '{0}' not found", gauge));
                    }
                    List<SensorReading> readings = allReadings[location.Id];
                    if (readings[0].Id > prevMaxReadingId)
                    {
                        if (readings[0].Id > result.MaxReadingId)
                        {
                            result.MaxReadingId = readings[0].Id;
                        }
                        result.Readings.Add(gauge, new ApiV2ReadingSet(readings, prevMaxReadingId));
                    }
                }
            }

            // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
            // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
            // quoting the way OkObjectResult does.
            return new ContentResult() { Content = JsonConvert.SerializeObject(result), ContentType = "application/json" };
        }
    }
}
