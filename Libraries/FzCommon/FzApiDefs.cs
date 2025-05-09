// All public classes used by APIs should be defined here.  The naming
// convention is a WIP...

// All classes called ApiFoo are intended for public consumption. No private information
// (e.g. Gage maintainer contact info) may appear. AdminApiFoo classes are only exposed
// in the admin tool system, and may include private info.

namespace FzCommon
{
    public class ApiFloodEvent
    {
        public int Id { get; set; }
        public string EventName { get; set; }

        // These are in region-local time and just include the date part.  'From' implies 
        // start-of-day and 'To' implies end-of-day, so a one-day event will have FromDate == ToDate.
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public ApiFloodEvent(FloodEventBase source)
        {
            this.Id = source.Id;
            this.EventName = source.EventName;
            this.FromDate = source.FromDate;
            this.ToDate = source.ToDate;
        }
    }

    public class ApiLocationInfo
    {
        // Id corresponds to SensorLocationBase.PublicLocationId
        public string Id { get; set; }
        public string LocationName { get; set; }
        public string ShortName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsOffline { get; set; }
        public double? Rank { get; set; }
        public double? MaxChangeThreshold { get; set; }
        public double? YMin { get; set; }
        public double? YMax { get; set; }
        public double? DischargeMin { get; set; }
        public double? DischargeMax { get; set; }
        public double? DischargeStageOne { get; set; }
        public double? DischargeStageTwo { get; set; }
        public double? GroundHeight { get; set; }
        public double? YellowStage { get; set; }
        public double? RedStage { get; set; }
        public double? RoadSaddleHeight { get; set; }
        public string RoadDisplayName { get; set; }
        public string DeviceTypeName { get; set; }
        public string TimeZoneName { get; set; }
        public bool HasDischarge { get; set; }
        public int UsgsSiteId { get; set; }
        public string NoaaSiteId { get; set; }

        // true if either IsOffline or no recent readings have come in
        public bool IsCurrentlyOffline { get; set; }

        public List<string> LocationImages { get; set; }

        public List<ApiFloodEvent> FloodEvents { get; set; }

        public ApiLocationInfo(SensorLocationBase location,
                               bool isCurrentlyOffline,
                               RegionBase region,
                               DeviceBase device,
                               DeviceType deviceType,
                               List<string> locationImages,
                               List<ApiFloodEvent> floodEvents,
                               UsgsSite usgsSite = null)
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
            this.DischargeMin = location.DischargeMin;
            this.DischargeMax = location.DischargeMax;
            this.DischargeStageOne = location.DischargeStageOne;
            this.DischargeStageTwo = location.DischargeStageTwo;
            this.GroundHeight = location.GroundHeight;
            this.YellowStage = location.Green;
            this.RedStage = location.Brown;
            this.RoadSaddleHeight = location.RoadSaddleHeight;
            this.RoadDisplayName = location.RoadDisplayName;
            this.IsCurrentlyOffline = isCurrentlyOffline;
            if (deviceType != null)
            {
                this.DeviceTypeName = deviceType.DeviceTypeName;
            }
            this.LocationImages = locationImages;
            this.FloodEvents = floodEvents;

            this.HasDischarge = false;
            if (location.DischargeMin.HasValue && location.DischargeMax.HasValue)
            {
                if (device != null)
                {
                    if (device.HasDischarge)
                    {
                        this.HasDischarge = true;
                    }
                }
            }

            if (usgsSite != null)
            {
                this.UsgsSiteId = usgsSite.SiteId;
                this.NoaaSiteId = usgsSite.NoaaSiteId;
            }

            this.TimeZoneName = region.IanaTimeZone;
        }

    }

    public enum ApiFloodLevel
    {
        Offline,
        Dry,
        Normal,
        NearFlooding,
        Flooding,
        Online,
    }

    public class ApiLocationStatus
    {
        public ApiGageReading LastReading;
        public ApiFloodLevel FloodLevel;
        public LevelTrend LevelTrend;
        public Trends WaterTrend;

        public ApiLocationStatus()
        {
        }

        public ApiLocationStatus(SensorLocationBase location, Trends trends, List<SensorReading> readings)
        {
            SensorReading latestReading = null;
            if (readings.Count > 1)
            {
                latestReading = readings[0];
                this.LastReading = new ApiGageReading(latestReading, location);
            }
            if (location.IsOffline || (latestReading == null))
            {
                this.FloodLevel = ApiFloodLevel.Offline;
                this.LevelTrend = LevelTrend.Offline;
                this.WaterTrend = null;
            }
            else
            {
                this.FloodLevel = ComputeFloodLevel(latestReading, location);

                this.WaterTrend = trends;
                if (this.WaterTrend != null)
                {
                    this.LevelTrend = this.WaterTrend.GetLevelTrend();
                }
                else
                {
                    this.LevelTrend = LevelTrend.Offline;
                }
            }
        }

        private static ApiFloodLevel ComputeFloodLevelFromThresholds(SensorReading reading, double? greenFeet, double? brownFeet, double? groundFeet)
        {
            if (reading == null)
            {
                return ApiFloodLevel.Offline;
            }
            if (greenFeet != null && brownFeet != null)
            {
                if (reading.WaterHeightFeet < greenFeet)
                {
                    if (groundFeet != null && (reading.WaterHeightFeet <= groundFeet))
                    {
                        return ApiFloodLevel.Dry;
                    }
                    else
                    {
                        return ApiFloodLevel.Normal;
                    }
                }
                else if (reading.WaterHeightFeet >= greenFeet && reading.WaterHeightFeet < brownFeet)
                {
                    return ApiFloodLevel.NearFlooding;
                }
                else
                {
                    return ApiFloodLevel.Flooding;
                }
            }
            else if (groundFeet != null && (reading.WaterHeightFeet <= groundFeet))
            {
                return ApiFloodLevel.Dry;
            }
            else
            {
                return ApiFloodLevel.Online;
            }
        }

        // uses the currently-configured status thresholds from the location
        public static ApiFloodLevel ComputeFloodLevel(SensorReading reading, SensorLocationBase location)
        {
            // assumes location.ConvertValuesForDisplay() has been called...
            return ComputeFloodLevelFromThresholds(reading, location.Green, location.Brown, location.GroundHeight);
        }

        // like the above, but uses the status thresholds that were snapshotted into the reading
        public static ApiFloodLevel ComputeReadingFloodLevel(SensorReading reading)
        {
            return ComputeFloodLevelFromThresholds(reading, reading.GreenASL, reading.BrownASL, reading.GroundHeightFeet);
        }

    }

    public class ApiGageReading
    {
        public DateTime Timestamp { get; set; }
        public double? WaterHeight { get; set; }
        public double? GroundHeight { get; set; }
        public double? WaterDischarge { get; set; }
        public int? BatteryMillivolt { get; set; }
        public double? BatteryPercent { get; set; }
        public double? RoadSaddleHeight { get; set; }
        public bool IsDeleted { get; set; }

        public ApiGageReading()
        {
        }

        public ApiGageReading(SensorReading source, SensorLocationBase location)
        {
            this.Timestamp = FzCommonUtility.ToRegionTimeFromUtc(source.Timestamp);
            this.WaterHeight = FzCommonUtility.GetRoundValue(source.WaterHeightFeet);
            this.GroundHeight = FzCommonUtility.GetRoundValue(source.GroundHeightFeet);
            this.WaterDischarge = FzCommonUtility.GetRoundValue(source.WaterDischarge);
            this.BatteryMillivolt = source.BatteryVolt;
            this.BatteryPercent = source.BatteryPercent;
            this.RoadSaddleHeight = FzCommonUtility.GetRoundValue(location.AdjustToFeetAboveSeaLevel(source.RoadSaddleHeight));
            this.IsDeleted = source.IsDeleted;
        }
    }

    public class ApiGageStatusAndRecentReadings
    {
        public string LocationId { get; set; }
        public ApiLocationStatus Status { get; set; }
        public List<ApiGageReading> Readings { get; set; } // newest first
    }

    public class ApiGageStatusAndRecentReadingsResponse
    {
        public List<ApiGageStatusAndRecentReadings> Gages { get; set; }
    }
}

