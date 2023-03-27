using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

using FzCommon;

namespace FloodzillaWeb.Controllers.Api
{
    public class LocationInfo
    {
        public string Id;
        public string LocationName;
        public string ShortName;
        public double? Latitude;
        public double? Longitude;
        public bool IsOffline;
        public double? Rank;
        public double? MaxChangeThreshold;
        public double? YMin;
        public double? YMax;
        public double? GroundHeight;
        public double? RoadSaddleHeight;
        public string RoadDisplayName;
        public string DeviceTypeName;
        public string TimeZoneName;
        public double? DischargeStageOne;
        public double? DischargeStageTwo;
        public string NoaaSiteId;

        public List<string> LocationImages = null;
        
        // If the caller requests status, this will be non-null.
        public LocationStatus CurrentStatus = null;

        // If locationDataOnly is false, this will be filled in.
        public List<GageReading> RecentReadings = null;

        public LocationInfo(SensorLocationBase location, RegionBase region, DeviceBase device, DeviceType deviceType, UsgsSite usgsSite)
        {
            this.Id = location.PublicLocationId;
            this.LocationName = location.LocationName;
            this.ShortName = location.ShortName;
            this.Latitude = location.Latitude;
            this.Longitude = location.Longitude;
            this.IsOffline = location.IsOffline;
            this.Rank = location.Rank;
            this.MaxChangeThreshold = location.MaxChangeThreshold;
            this.YMin = location.YMin;
            this.YMax = location.YMax;
            this.GroundHeight = location.GroundHeight;
            this.RoadSaddleHeight = location.RoadSaddleHeight;
            this.RoadDisplayName = location.RoadDisplayName;
            this.DischargeStageOne = location.DischargeStageOne;
            this.DischargeStageTwo = location.DischargeStageTwo;
            if (deviceType != null)
            {
                this.DeviceTypeName = deviceType.DeviceTypeName;
            }
            if (usgsSite != null)
            {
                this.NoaaSiteId = usgsSite.NoaaSiteId;
            }

            //$ TODO: This should eventually come from the region...
            this.TimeZoneName = FzCommonUtility.IanaTimeZone;
        }
    }

    public class MetagageInfo
    {
        public string Id;
        public string SiteId;
        public string Name;
        public string ShortName;
        public double StageOne;
        public double StageTwo;
    }

    public enum FloodLevel
    {
        Offline,
        Dry,
        Normal,
        NearFlooding,
        Flooding,
        Online,
    }

    public class LocationStatus
    {
        public GageReading LastReading;
        public FloodLevel FloodLevel;
        public LevelTrend LevelTrend;
        public Trends WaterTrend;
    }

    //$ TODO: Should timestamp be ToUnixTimeMilliseconds() like old API uses for x axis?
    // All height-related values are in feet above sea level
    public class GageReading
    {
        public DateTime Timestamp;
        public double? WaterHeight;
        public double? GroundHeight;
        public double? WaterDischarge;
        public int? BatteryMillivolt;
        public double? RoadSaddleHeight;
        public bool IsDeleted;

        public GageReading(SensorReading source, SensorLocationBase location)
        {
            this.Timestamp = FzCommonUtility.ToRegionTimeFromUtc(source.Timestamp);
            this.WaterHeight = FzCommonUtility.GetRoundValue(source.WaterHeightFeet);
            this.GroundHeight = FzCommonUtility.GetRoundValue(source.GroundHeightFeet);
            this.WaterDischarge = FzCommonUtility.GetRoundValue(source.WaterDischarge);
            this.BatteryMillivolt = source.BatteryVolt;
            this.RoadSaddleHeight = FzCommonUtility.GetRoundValue(location.AdjustToFeetAboveSeaLevel(source.RoadSaddleHeight));
            this.IsDeleted = source.IsDeleted;
        }
    }

    public class GeoJsonGeometry
    {
        public string Type;
        public double[] Coordinates;
        public GeoJsonGeometry(LocationInfo location)
        {
            this.Type = "Point";
            this.Coordinates = new double[] { location.Longitude ?? 0, location.Latitude ?? 0 };
        }
    }
    
    public class GeoJsonLocationInfo
    {
        public string Id;
        public string Type;
        public GeoJsonGeometry Geometry;
        public LocationInfo Properties;
        public GeoJsonLocationInfo(LocationInfo location)
        {
            this.Id = location.Id;
            this.Type = "Feature";
            this.Geometry = new GeoJsonGeometry(location);
            this.Properties = location;
        }
    }

    public class GeoJsonFeatureCollection
    {
        public string Type;
        public List<GeoJsonLocationInfo> Features;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [AllowAnonymous]
    [Route("api/Client/[action]")]
    [JwtHeaderFilter]
    public class ClientApiController : Controller
    {

        public const bool AdminsCanSeePrivateLocations = true;

        //$ TODO: Other params?
        public ClientApiController()
        {
        }

        // This controls how long a trend is shown for offline gages.
        private const int RecentReadingHours = 48;

        public async Task<IActionResult> GetLocations(int regionId, bool locationDataOnly, string id = null, bool showDeletedReadings = false, bool geoJson = false)
        {
            List<SensorLocationBase> locations;
            List<DeviceBase> devices;
            List<DeviceType> deviceTypes;
            List<FloodzillaWeb.Models.FzModels.Uploads> uploads;
            List<UsgsSite> usgsSites;
            List<LocationInfo> ret = new List<LocationInfo>();

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                RegionBase region = RegionBase.GetRegion(sqlcn, regionId);

                //$ TODO: Caching if necessary
                locations = SensorLocationBase.GetLocationsForRegion(sqlcn, regionId);
                devices = DeviceBase.GetDevices(sqlcn);
                deviceTypes = DeviceType.GetDeviceTypes(sqlcn);
                uploads = FloodzillaWeb.Models.FzModels.Uploads.GetUploads(sqlcn);
                usgsSites = await UsgsSite.GetUsgsSites(sqlcn);

                // For right now, these are the same; if we don't need to go back 48 hours for trends,
                // that could change...
                int hourOffset = -RecentReadingHours;
                if (!locationDataOnly)
                {
                    hourOffset = -48;
                }
                Dictionary<int, List<SensorReading>> recentReadings = await SensorReading.GetAllRecentReadings(sqlcn, DateTime.UtcNow.AddHours(hourOffset), showDeletedReadings);

                // This will give latest-reading dates for readings older than 48 hours...
                List<SensorReading> latestReadings = await SensorReading.GetLatestReadingsByLocation(sqlcn, regionId);

                var filteredlocs = locations.Where(l => !l.IsDeleted);
                if (!User.IsInRole("Admin"))
                {
                    filteredlocs = filteredlocs.Where(l => (l.IsActive && l.IsPublic));
                }
                
                if (!String.IsNullOrEmpty(id))
                {
                    filteredlocs = locations.Where(l => l.PublicLocationId == id);
                }
                var sortedlocs = filteredlocs.OrderByDescending(l=>l.Rank.HasValue).ThenBy(l=>l.Rank);
                foreach (SensorLocationBase location in sortedlocs)
                {
                    DeviceBase device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                    if (device == null)
                    {
                        continue;
                    }
                    DeviceType deviceType = deviceTypes.FirstOrDefault(dt => dt.DeviceTypeId == device.DeviceTypeId);
                    UsgsSite usgsSite = null;

                    SensorReading latestReading = latestReadings.FirstOrDefault(sr => sr.LocationId == location.Id);
                    List<SensorReading> recent = null;
                    if (recentReadings.ContainsKey(location.Id))
                    {
                        recent = recentReadings[location.Id];
                    }

                    if (device.DeviceTypeId == DeviceTypeIds.Usgs)
                    {
                        usgsSite = usgsSites.FirstOrDefault(site => site.SiteId == device.DeviceId);
                    }

                    LocationInfo li = this.CreateLocationInfo(location, region, device, deviceType, usgsSite, uploads, latestReading, recent);
                    if (!locationDataOnly && (recent != null))
                    {
                        li.RecentReadings = new List<GageReading>();
                        foreach (SensorReading sr in recent)
                        {
                            li.RecentReadings.Add(new GageReading(sr, location));
                        }
                    }
                    
                    ret.Add(li);
                }

                sqlcn.Close();
            }
            
            // NOTE: Don't use JsonResult() here -- it doesn't pick up the default serializer settings...
            if (geoJson)
            {
                Response.Headers.Add("Content-Disposition", "attachment; filename=geojson.json");
                return Ok(JsonConvert.SerializeObject(BuildGeoJsonResponse(ret), Formatting.Indented));
            }
            else
            {
                return Ok(JsonConvert.SerializeObject(ret));
            }
        }

        public IActionResult GetMetagages(int regionId)
        {
            List<MetagageInfo> ret = new List<MetagageInfo>();
            for (int i = 0; i < Metagages.MetagageIds.Length; i++)
            {
                ret.Add(new MetagageInfo()
                {
                    Id = Metagages.MetagageIds[i],
                    SiteId = Metagages.MetagageSiteIds[i],
                    Name = Metagages.MetagageNames[i],
                    ShortName = Metagages.MetagageShortNames[i],
                    StageOne = Metagages.MetagageStageOnes[i],
                    StageTwo = Metagages.MetagageStageTwos[i],
                });
            }

            // NOTE: Don't use JsonResult() here -- it doesn't pick up the default serializer settings...
            return Ok(JsonConvert.SerializeObject(ret));
        }

        private GeoJsonFeatureCollection BuildGeoJsonResponse(List<LocationInfo> locations)
        {
            List<GeoJsonLocationInfo> geolocs = new List<GeoJsonLocationInfo>();
            foreach (LocationInfo location in locations)
            {
                geolocs.Add(new GeoJsonLocationInfo(location));
            }
            GeoJsonFeatureCollection response = new GeoJsonFeatureCollection();
            response.Type = "FeatureCollection";
            response.Features = geolocs;
            return response;
        }

        
        //$ TODO: Add flags to this to request specific types of data (e.g. battery level, water discharge, etc)?

        //$ TODO: Error handling

        // If lastReadingId is provided and there are no new readings, this will return a 200 with just
        // "noData: true".
        public async Task<IActionResult> GetGageReadings(int regionId,
                                                         string id,
                                                         DateTime? fromDateTime = null,
                                                         DateTime? toDateTime = null,
                                                         bool showDeletedReadings = false,
                                                         int lastReadingId = 0)
        {
            // Assume passed-in dates are in region time; convert to UTC
            //$ TODO: use the region to do the conversions
            if (fromDateTime.HasValue)
            {
                fromDateTime = FzCommonUtility.ToUtcFromRegionTime(new DateTime(fromDateTime.Value.Ticks, DateTimeKind.Unspecified));
            }
            if (toDateTime.HasValue)
            {
                toDateTime = FzCommonUtility.ToUtcFromRegionTime(new DateTime(toDateTime.Value.Ticks, DateTimeKind.Unspecified));
            }
            return await GetGageReadingsCore(regionId, id, fromDateTime, toDateTime, showDeletedReadings, lastReadingId);
        }

        public async Task<IActionResult> GetGageReadingsUTC(int regionId,
                                                            string id,
                                                            DateTime? fromDateTime = null,
                                                            DateTime? toDateTime = null,
                                                            bool showDeletedReadings = false,
                                                            int lastReadingId = 0)
        {
            // This is insanely dumb.  The default ASP.NET query-string binding glue
            // converts passed-in DateTime objects to server local time (i.e. the value
            // you get depends on the timezone setting of the server) even if the
            // passed-in string explicitly declares itself to be UTC.  So we have to unconvert
            // values back to UTC.
            
            if (fromDateTime.HasValue)
            {
                fromDateTime = fromDateTime.Value.ToUniversalTime();
            }
            if (toDateTime.HasValue)
            {
                toDateTime = toDateTime.Value.ToUniversalTime();
            }
            return await GetGageReadingsCore(regionId, id, fromDateTime, toDateTime, showDeletedReadings, lastReadingId);
        }

        private async Task<IActionResult> GetGageReadingsCore(int regionId,
                                                              string id,
                                                              DateTime? fromDateTime = null,
                                                              DateTime? toDateTime = null,
                                                              bool showDeletedReadings = false,
                                                              int lastReadingId = 0)
        {
            SensorLocationBase location = null;
            List<DeviceBase> devices;
            List<DeviceType> deviceTypes;
            List<FloodzillaWeb.Models.FzModels.Uploads> uploads;
            List<UsgsSite> usgsSites;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                location = SensorLocationBase.GetLocationByPublicId(sqlcn, id);
                if (location == null)
                {
                    //$ TODO: Log this somewhere?
                    return NotFound(String.Format("Location '{0}' not found", id));
                }

                //$ TODO: Versions of GetReadingsForLocation() which take sqlconnection param
                List<GageReading> gageReadings = new List<GageReading>();
                List<SensorReading> readings;
                if (showDeletedReadings)
                {
                    readings = await SensorReading.GetAllReadingsForLocation(
                        location.Id,
                        null,
                        fromDateTime,
                        toDateTime,
                        0, // skipCount
                        lastReadingId);
                }
                else
                {
                    readings = await SensorReading.GetReadingsForLocation(
                        location.Id,
                        null,
                        fromDateTime,
                        toDateTime,
                        0, // skipCount
                        lastReadingId);
                }
                    
                if (lastReadingId > 0 && readings.Count == 0)
                {
                    // Special-case this.
                    return Ok(new { noData = true });
                }

                RegionBase region = RegionBase.GetRegion(sqlcn, regionId);

                //$ TODO: Caching if necessary
                devices = DeviceBase.GetDevices(sqlcn);
                deviceTypes = DeviceType.GetDeviceTypes(sqlcn);
                uploads = FloodzillaWeb.Models.FzModels.Uploads.GetUploads(sqlcn);
                usgsSites = await UsgsSite.GetUsgsSites(sqlcn);

                DeviceBase device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                if (device == null)
                {
                    //$ TODO: Log this somewhere?
                    return NotFound(String.Format("Location '{0}' does not have a device", id));
                }
                DeviceType deviceType = deviceTypes.FirstOrDefault(dt => dt.DeviceTypeId == device.DeviceTypeId);
                UsgsSite usgsSite = null;

                List<SensorReading> recent = null;
                if (lastReadingId == 0 && (!toDateTime.HasValue || toDateTime > DateTime.UtcNow))
                {
                    // The data we already fetched can be used for 'recent'.
                    recent = readings;
                }
                else
                {
                    // We have to do a separate request to get readings to use for the LocationInfo.
                    recent = await SensorReading.GetReadingsForLocation(
                        location.Id,
                        CLIRecentReadingCount,
                        DateTime.UtcNow.AddHours(-RecentReadingHours),
                        null);
                }
                SensorReading latestReading = null;
                if (recent != null && recent.Count > 0)
                {
                    latestReading = recent[0];
                }

                if (device.DeviceTypeId == DeviceTypeIds.Usgs)
                {
                    usgsSite = usgsSites.FirstOrDefault(site => site.SiteId == device.DeviceId);
                }
                LocationInfo li = this.CreateLocationInfo(location, region, device, deviceType, usgsSite, uploads, latestReading, recent);

                int lastNewReadingId = 0;
                if (readings.Count > 0)
                {
                    lastNewReadingId = readings[0].Id;
                }

                foreach (SensorReading reading in readings)
                {
                    gageReadings.Add(new GageReading(reading, location));
                }

                dynamic result = new
                {
                    Readings = gageReadings,
                    LastReadingId = lastNewReadingId,
                    Gage = li,
                };
                sqlcn.Close();

                // NOTE: Don't use JsonResult() here -- it doesn't pick up the default serializer settings...
                return Ok(JsonConvert.SerializeObject(result));
            }
        }

        private static int GetOfflineThresholdMinutes(DeviceBase device)
        {
            //$ TODO: This is currently configurable, but all existing sensors use
            //$ these settings.  When we get rid of the DevicesConfiguration table,
            //$ this code should get the reading interval from Devices (or wherever
            //$ we end up putting it)
            switch (device.DeviceTypeId)
            {
                case DeviceTypeIds.Svpa:
                case DeviceTypeIds.Senix:
                    return 2 * 15;
                case DeviceTypeIds.Usgs:
                    return 2 * 360;
            }
            return 0;
        }

        // 'recent' in this function is only used for trend calculation, so we don't need more than that many readings...
        private const int CLIRecentReadingCount = Trends.DesiredReadingCount;
        private LocationInfo CreateLocationInfo(SensorLocationBase location,
                                                RegionBase region,
                                                DeviceBase device,
                                                DeviceType deviceType,
                                                UsgsSite usgsSite,
                                                List<FloodzillaWeb.Models.FzModels.Uploads> uploads,
                                                SensorReading latestReading,
                                                List<SensorReading> recent)
        {
            if (!location.IsOffline)
            {
                if (latestReading == null)
                {
                    location.IsOffline = true;
                }
                else
                {
                    DateTime lastReading = latestReading.Timestamp;
                    var minutesSinceReading = (DateTime.UtcNow - lastReading).TotalMinutes;

                    int threshold = GetOfflineThresholdMinutes(device);

                    if (threshold > 0)
                    {
                        if (minutesSinceReading > threshold)
                        {
                            location.IsOffline = true;
                        }
                    }
                }
            }

            location.ConvertValuesForDisplay();
            
            LocationInfo li = new LocationInfo(location, region, device, deviceType, usgsSite);
            var locationUploads = uploads.Where(u => u.LocationId == location.Id);
            if (locationUploads.Count() > 0)
            {
                li.LocationImages = new List<string>();
                foreach (var u in locationUploads)
                {
                    li.LocationImages.Add(u.Image);
                }
            }

            //$ TODO: Consider making status-related stuff optional in this routine...
            li.CurrentStatus = new LocationStatus()
            {
                LastReading = (latestReading == null) ? null : new GageReading(latestReading, location),
            };
            if (location.IsOffline || (latestReading == null))
            {
                li.CurrentStatus.FloodLevel = FloodLevel.Offline;
                li.CurrentStatus.LevelTrend = LevelTrend.Offline;
                li.CurrentStatus.WaterTrend = null;
            }
            else
            {
                if (location.Green != null && location.Brown != null)
                {
                    if (latestReading.WaterHeightFeet < location.Green)
                    {
                        if (location.GroundHeight != null && (latestReading.WaterHeightFeet <= location.GroundHeight))
                        {
                            li.CurrentStatus.FloodLevel = FloodLevel.Dry;
                        }
                        else
                        {
                            li.CurrentStatus.FloodLevel = FloodLevel.Normal;
                        }
                    }
                    else if (latestReading.WaterHeightFeet >= location.Green && latestReading.WaterHeightFeet < location.Brown)
                    {
                        li.CurrentStatus.FloodLevel = FloodLevel.NearFlooding;
                    }
                    else
                    {
                        li.CurrentStatus.FloodLevel = FloodLevel.Flooding;
                    }
                }
                else if (location.GroundHeight != null && (latestReading.WaterHeightFeet <= location.GroundHeight))
                {
                    li.CurrentStatus.FloodLevel = FloodLevel.Dry;
                }
                else
                {
                    li.CurrentStatus.FloodLevel = FloodLevel.Online;
                }

                if (recent != null)
                {
                    li.CurrentStatus.WaterTrend = TrendCalculator.CalculateWaterTrends(recent);
                }
                if (li.CurrentStatus.WaterTrend != null)
                {
                    li.CurrentStatus.LevelTrend = li.CurrentStatus.WaterTrend.GetLevelTrend();
                }
                else
                {
                    li.CurrentStatus.LevelTrend = LevelTrend.Offline;
                }
            }

            return li;
        }

        //
        // "New" API: separating location info from location status.
        //
        // NOTE: Don't just return a List<ApiLocationInfo> -- it will ignore
        // our default serialization settings.
        public async Task<IActionResult> APIGetLocationInfo(int regionId)
        {
            //$ TODO: How do we want to handle exceptions in this?
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                RegionBase region = RegionBase.GetRegion(sqlcn, regionId);

                //$ TODO: Caching if necessary
                List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsForRegionAsync(sqlcn, regionId);
                List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
                List<DeviceType> deviceTypes = DeviceType.GetDeviceTypes(sqlcn);
                List<FloodzillaWeb.Models.FzModels.Uploads> uploads = FloodzillaWeb.Models.FzModels.Uploads.GetUploads(sqlcn);
                List<FloodEventBase> floodEvents = await FloodEventBase.GetFloodEvents(sqlcn);
                List<FloodEventLocationData> floodEventLocationData = await FloodEventLocationData.GetAllFloodEventLocationData(sqlcn);
                List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);

                floodEvents.Sort((a, b) => b.FromDate.CompareTo(a.FromDate));
                        
                List<ApiLocationInfo> ret = new List<ApiLocationInfo>();

                DateTime now = DateTime.UtcNow;
                DateTime recent = now.AddHours(-24);
                Dictionary<int, DateTime> recentReadingTimes = await SensorReading.GetRecentSensorReadingTimestampsByLocationAsync(sqlcn, regionId, recent, now);

                var filteredlocs = locations.Where(l => !l.IsDeleted);
                if (!AdminsCanSeePrivateLocations || !User.IsInRole("Admin"))
                {
                    filteredlocs = filteredlocs.Where(l => (l.IsActive && l.IsPublic));
                }

                var sortedlocs = filteredlocs.OrderByDescending(l=>l.Rank.HasValue).ThenBy(l=>l.Rank);
                foreach (SensorLocationBase location in sortedlocs)
                {
                    //$ TODO: If these are cached, do this in a local copy
                    location.ConvertValuesForDisplay();

                    DeviceBase device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                    if (device == null)
                    {
                        continue;
                    }

                    // Mark gage as currently offline if there are no readings within past 24 hours.
                    bool isCurrentlyOffline = false;
                    if (location.IsOffline)
                    {
                        isCurrentlyOffline = true;
                    }
                    else
                    {
                        isCurrentlyOffline = !recentReadingTimes.ContainsKey(location.Id);
                    }
                    DeviceType deviceType = deviceTypes.FirstOrDefault(dt => dt.DeviceTypeId == device.DeviceTypeId);
                    var locationUploads = uploads.Where(u => u.LocationId == location.Id);
                    List<string> locationImages = null;
                    if (locationUploads.Count() > 0)
                    {
                        locationImages = new List<string>();
                        foreach (var u in locationUploads)
                        {
                            locationImages.Add(u.Image);
                        }                        
                    }

                    List<ApiFloodEvent> floods = null;
                    var locationFloods = floodEventLocationData.Where(d => d.LocationId == location.Id);
                    if (locationFloods.Count() > 0)
                    {
                        floods = new List<ApiFloodEvent>();
                        foreach (FloodEventBase flood in floodEvents.Where(e => e.IsActive && e.IsPublic && !e.IsDeleted))
                        {
                            if (locationFloods.Any(d => d.EventId == flood.Id))
                            {
                                floods.Add(new ApiFloodEvent(flood));
                            }
                        }
                    }

                    UsgsSite usgsSite = null;
                    if (deviceType.DeviceTypeId == DeviceTypeIds.Usgs)
                    {
                        usgsSite = usgsSites.FirstOrDefault(site => site.SiteId == device.DeviceId);
                    }
                    
                    ApiLocationInfo ali = new ApiLocationInfo(location, isCurrentlyOffline, region, device, deviceType, locationImages, floods, usgsSite);
                    ret.Add(ali);
                }
                sqlcn.Close();

                // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
                // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
                // quoting the way OkObjectResult does.
                return new ContentResult() { Content = JsonConvert.SerializeObject(ret), ContentType = "application/json" };
            }
        }   
    }
}
