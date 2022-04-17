using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using FzCommon;

namespace ReadingSvc
{
    // All height-related values are in feet above sea level
    public class GageReading
    {
        public int Id;
        public DateTime Timestamp;
        public double? WaterHeight;
        public double? GroundHeight;
        public double? WaterDischarge;
        public int? BatteryMillivolt;
        public double? RoadSaddleHeight;
        public double? RSSI;
        public double? SNR;
        public bool IsDeleted;

        // This is true if this is a 'fake' reading to indicate that we
        // didn't receive an expected update
        public bool IsMissing;

        public GageReading()
        {
        }

        //$ TODO: don't do timezone conversion here
        public GageReading(SensorReading source, SensorLocationBase location, bool returnUtc)
        {
            this.Id = source.Id;
            if (returnUtc)
            {
                this.Timestamp = source.Timestamp;
            }
            else
            {
                this.Timestamp = FzCommonUtility.ToRegionTimeFromUtc(source.Timestamp);
            }
            this.WaterHeight = FzCommonUtility.GetRoundValue(source.WaterHeightFeet);
            this.GroundHeight = FzCommonUtility.GetRoundValue(source.GroundHeightFeet);
            this.WaterDischarge = FzCommonUtility.GetRoundValue(source.WaterDischarge);
            this.BatteryMillivolt = source.BatteryVolt;
            this.RoadSaddleHeight = FzCommonUtility.GetRoundValue(location.AdjustToFeetAboveSeaLevel(source.RoadSaddleHeight));
            this.RSSI = source.RSSI;
            this.SNR = source.SNR;
            this.IsDeleted = source.IsDeleted;

            //$ TODO: obviously this is a hack
            this.IsMissing = (source.Id < 0);
        }
    }

    //$ TODO: Get rid of GageReading, and fold IsMissing into ApiGageReading?
    public class ReadingStoreResponse
    {
        public List<GageReading> Readings;
        public int LastReadingId;
        public bool NoData;
        public ApiLocationStatus Status;
        public ApiLocationStatus PeakStatus;
        public List<ApiGageReading> Predictions;
        public NoaaForecast NoaaForecast;
        public double PredictedFeetPerHour;
        public double PredictedCfsPerHour;

        // Temporary, for debugging, to compare predictions to reality.
        public List<ApiGageReading> ActualReadings;
    }

    public sealed class ReadingStore
    {
        public static ReadingStore Store { get { return s_lazy.Value; }}

        //$ TODO: Add flags to this to request specific types of data (e.g. battery level, water discharge, etc)?
        //$ TODO: Error handling

        public async Task<ReadingStoreResponse> GetGageReadings(int regionId,
                                                                string id,
                                                                DateTime? fromDateTime = null,
                                                                DateTime? toDateTime = null,
                                                                bool showDeletedReadings = false,
                                                                bool showMissingReadings = false,
                                                                bool getMinimalReadings = false,
                                                                bool includeStatus = false,
                                                                bool includePredictions = false,
                                                                bool includeForecast = false, 
                                                                int lastReadingId = 0,
                                                                bool returnUtc = false)
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
            return await GetGageReadingsCore(regionId, id, fromDateTime, toDateTime, showDeletedReadings, showMissingReadings, getMinimalReadings, includeStatus, includePredictions, includeForecast, lastReadingId, returnUtc);
        }

        public async Task<ReadingStoreResponse> GetGageReadingsUTC(int regionId,
                                                                   string id,
                                                                   DateTime? fromDateTime = null,
                                                                   DateTime? toDateTime = null,
                                                                   bool showDeletedReadings = false,
                                                                   bool showMissingReadings = false,
                                                                   bool getMinimalReadings = false,
                                                                   bool includeStatus = false,
                                                                   bool includePredictions = false,
                                                                   bool includeForecast = false, 
                                                                   int lastReadingId = 0,
                                                                   bool returnUtc = false)
        {
            return await GetGageReadingsCore(regionId, id, fromDateTime, toDateTime, showDeletedReadings, showMissingReadings, getMinimalReadings, includeStatus, includePredictions, includeForecast, lastReadingId, returnUtc);
        }

        private async Task<ReadingStoreResponse> GetGageReadingsCore(int regionId,
                                                                     string id,
                                                                     DateTime? fromDateTime,
                                                                     DateTime? toDateTime,
                                                                     bool showDeletedReadings,
                                                                     bool showMissingReadings,
                                                                     bool getMinimalReadings,
                                                                     bool includeStatus,
                                                                     bool includePredictions,
                                                                     bool includeForecast, 
                                                                     int lastReadingId,
                                                                     bool returnUtc)
        {
            SensorLocationBase location = null;
            List<SensorReading> readings = null;
            DeviceBase device = null;
            UsgsSite usgsSite = null;
            NoaaForecast noaaForecast = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                location = SensorLocationBase.GetLocationByPublicId(sqlcn, id);
                if (location == null)
                {
                    //$ TODO: Log this somewhere?
                    //$ TODO: Pass this error out to API? or move parsing out to API?
                    return null;
                }

                if (includeForecast)
                {
                    List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
                    device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                    if (device != null)
                    {
                        if (device.DeviceTypeId == DeviceTypeIds.Usgs)
                        {
                            usgsSite = await UsgsSite.GetUsgsSite(sqlcn, device.DeviceId);
                        }
                        if (usgsSite != null)
                        {
                            noaaForecast = await NoaaForecast.GetLatestSavedForecast(sqlcn, usgsSite.NoaaSiteId);
                            if (noaaForecast != null)
                            {
                                if (!returnUtc)
                                {
                                    RegionBase region = await RegionBase.GetRegionAsync(sqlcn, regionId);
                                    noaaForecast.Created = region.ToRegionTimeFromUtc(noaaForecast.Created);
                                    foreach (NoaaForecastItem item in noaaForecast.Data)
                                    {
                                        if (item.Discharge.HasValue)
                                        {
                                            item.Discharge = Math.Round(item.Discharge.Value);
                                        }
                                        item.Timestamp = region.ToRegionTimeFromUtc(item.Timestamp);
                                    }
                                }
                            }
                        }
                    }
                }
                
                sqlcn.Close();

                location.ConvertValuesForDisplay();

                readings = await s_cache.GetReadingsForLocation(location.Id,
                                                                location.PublicLocationId,
                                                                showDeletedReadings,
                                                                showMissingReadings,
                                                                getMinimalReadings,
                                                                null,
                                                                fromDateTime,
                                                                toDateTime,
                                                                0,
                                                                lastReadingId);
                    
                if (lastReadingId > 0 && readings.Count == 0)
                {
                    // Special-case this.
                    return new ReadingStoreResponse()
                    {
                        Readings = null,
                        NoData = true,
                        LastReadingId = lastReadingId,
                    };
                }

                int lastNewReadingId = 0;
                if (readings.Count > 0)
                {
                    lastNewReadingId = readings[0].Id;
                }

                List<GageReading> gageReadings = new List<GageReading>();
                double peakValue = Double.MinValue;
                SensorReading peakReading = null;
                foreach (SensorReading reading in readings)
                {
                    if (reading.WaterHeightFeet.HasValue && reading.WaterHeightFeet > peakValue)
                    {
                        peakValue = reading.WaterHeightFeet.Value;
                        peakReading = reading;
                    }
                    gageReadings.Add(new GageReading(reading, location, returnUtc));
                }

                // Just a note here: "peak" and "crest" aren't quite the same thing.  A "crest"
                // has more rules (e.g. it can't be the first or last reading, it must be >1 foot
                // greater than the minimum, etc).  "peak" is just the highest reading in a given set.
                ApiLocationStatus peakStatus = null;
                if (includeStatus && (peakReading != null))
                {
                    peakStatus = new ApiLocationStatus()
                    {
                        LastReading = new ApiGageReading(peakReading, location),
                        FloodLevel = ApiLocationStatus.ComputeFloodLevel(peakReading, location),

                        // this is kind of a lie, but really this is meaningless and we don't want
                        // to default to "offline" because that's even more of a lie.
                        LevelTrend = LevelTrend.Steady,
                    };
                }

                ApiLocationStatus status = null;
                List<ApiGageReading> predictions = null;
                List<ApiGageReading> actualReadings = null;
                double feetPerHour = 0;
                double cfsPerHour = 0;
                if (includeStatus || includePredictions)
                {
                    // if this was a partial reading, do an extra fetch so we can fully calculate status
                    // and/or predictions
                    List<SensorReading> latestReadings;
                    DateTime latestTo = toDateTime.HasValue ? toDateTime.Value : DateTime.UtcNow;
                    DateTime latestFrom = latestTo.AddMinutes(-Trends.AgeThresholdMinutes);
                    latestReadings = await s_cache.GetReadingsForLocation(location.Id,
                                                                          location.PublicLocationId,
                                                                          false,
                                                                          false,
                                                                          getMinimalReadings,
                                                                          Trends.DesiredReadingCount,
                                                                          latestFrom,
                                                                          latestTo,
                                                                          0,
                                                                          0);
                    Trends trends = TrendCalculator.CalculateWaterTrends(latestReadings);
                    Trends dischargeTrends = TrendCalculator.CalculateDischargeTrends(latestReadings);
                    if (includeStatus)
                    {
                        status = new ApiLocationStatus(location, trends, latestReadings);
                    }
                    if (includePredictions)
                    {
                        predictions = TrendCalculator.CalculatePredictions(trends, dischargeTrends, readings != null ? readings : latestReadings, out feetPerHour, out cfsPerHour);
                        if (predictions != null && predictions.Count > 0)
                        {
                            if (!returnUtc)
                            {
                                foreach (ApiGageReading agr in predictions)
                                {
                                    agr.Timestamp = FzCommonUtility.ToRegionTimeFromUtc(agr.Timestamp);
                                }
                            }

                            //$ TODO: Get rid of this when we no longer need it....
                            List<SensorReading> actual;
                            actual = await s_cache.GetReadingsForLocation(location.Id,
                                                                          location.PublicLocationId,
                                                                          false,
                                                                          false,
                                                                          getMinimalReadings,
                                                                          0,
                                                                          latestTo,
                                                                          latestTo.AddMinutes(Trends.PredictionIntervalMinutes * Trends.PredictionCount),
                                                                          0,
                                                                          0);
                            if (actual != null && actual.Count > 0)
                            {
                                actual.Reverse();
                                actualReadings = new List<ApiGageReading>();
                                foreach (SensorReading sr in actual)
                                {
                                    actualReadings.Add(new ApiGageReading(sr, location));
                                }
                            }
                        }
                    }
                }

                return new ReadingStoreResponse()
                {
                    Readings = gageReadings,
                    LastReadingId = lastNewReadingId,
                    Status = status,
                    PeakStatus = peakStatus,
                    Predictions = predictions,
                    NoaaForecast = noaaForecast,
                    PredictedFeetPerHour = feetPerHour,
                    PredictedCfsPerHour = cfsPerHour,
                    ActualReadings = actualReadings,
                };
            }
        }

        public async Task<bool> MarkReadingAsDeleted(string locationPublicId, int readingId)
        {
            SensorReading sr = await GetSensorReading(locationPublicId, readingId);
            if (sr == null)
            {
                return false;
            }
            if (sr.IsDeleted)
            {
                return false;
            }
            return await s_cache.MarkReadingAsDeleted(locationPublicId, readingId, sr);
        }

        public async Task<bool> MarkReadingAsUndeleted(string locationPublicId, int readingId)
        {
            SensorReading sr = await GetSensorReading(locationPublicId, readingId);
            if (sr == null)
            {
                return false;
            }
            if (!sr.IsDeleted)
            {
                return false;
            }
            return await s_cache.MarkReadingAsUndeleted(locationPublicId, readingId, sr);
        }

        private async Task<SensorReading> GetSensorReading(string locationPublicId, int readingId)
        {
            return await s_cache.GetSensorReading(locationPublicId, readingId);
        }

        private static ReadingCache s_cache;
        private static readonly Lazy<ReadingStore> s_lazy
                = new Lazy<ReadingStore>(() => new ReadingStore());
        private ReadingStore()
        {
            s_cache = new ReadingCache();
        }
    }
}