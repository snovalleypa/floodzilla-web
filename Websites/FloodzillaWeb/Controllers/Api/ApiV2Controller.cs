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

    public class ApiV2Forecast
    {
        public string NoaaSiteId { get; }
        public DateTime ForecastCreated { get; }
        public int ForecastId { get; }
        public DateTime[] Timestamps { get; }
        public double?[]? WaterHeights { get; }
        public double?[] Discharges { get; }
        public ApiV2Forecast(NoaaForecast noaaForecast, bool isMetagauge)
        {
            this.NoaaSiteId = noaaForecast.NoaaSiteId;
            this.ForecastId = noaaForecast.ForecastId;
            this.ForecastCreated = new DateTime(noaaForecast.Created.Ticks, DateTimeKind.Utc);
            this.Timestamps = new DateTime[noaaForecast.Data.Count];
            if (!isMetagauge)
            {
                this.WaterHeights = new double?[noaaForecast.Data.Count];
            }
            this.Discharges = new double?[noaaForecast.Data.Count];
            for (int i = 0; i < noaaForecast.Data.Count; i++)
            {
                this.Timestamps[i] = new DateTime(noaaForecast.Data[i].Timestamp.Ticks, DateTimeKind.Utc);
                if (!isMetagauge)
                {
                    this.WaterHeights[i] = noaaForecast.Data[i].Stage;
                }
                this.Discharges[i] = noaaForecast.Data[i].Discharge;
            }
        }
    }

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
        public ApiV2ForecastExp(NoaaForecast noaaForecast, bool isMetagauge)
        {
            this.NoaaSiteId = noaaForecast.NoaaSiteId;
            this.ForecastId = noaaForecast.ForecastId;
            this.ForecastCreated = new DateTime(noaaForecast.Created.Ticks, DateTimeKind.Utc);
            this.Data = new ApiV2Forecast_DataPoint[noaaForecast.Data.Count];
            for (int i = 0; i < noaaForecast.Data.Count; i++)
            {
                this.Data[i] = new ApiV2Forecast_DataPoint()
                {
                    Timestamp = new DateTime(noaaForecast.Data[i].Timestamp.Ticks, DateTimeKind.Utc),
                    Discharge = noaaForecast.Data[i].Discharge,
                };
                if (!isMetagauge)
                {
                    this.Data[i].WaterHeight = noaaForecast.Data[i].Stage;
                }
            }
        }
    }

    public class ApiV2ForecastResponse : Dictionary<string, ApiV2Forecast?>
    {
    }

    public class ApiV2ForecastResponseExp : Dictionary<string, ApiV2ForecastExp?>
    {
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
    }
}
