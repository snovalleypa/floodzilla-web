//#define SAVE_DETAILED_STATUS

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;
using FzCommon.Processors;

namespace FloodzillaJob
{
    public class GageEventDetector : FloodzillaAzureJob
    {
        // If there's a gap bigger than this before the most recent gage reading, we
        // won't detect any threshold crossings...
        public const int RecentHours = 3;
        
        public static async Task DetectGageEvents(ILogger log)
        {
            JobRunLog runLog = new JobRunLog("FloodzillaJob.DetectGageEvents");
            StringBuilder sbDetails = new StringBuilder();

            try
            {
                BlobContainerClient container = await EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
                GageEventDetectorStatus lastStatus = await GageEventDetectorStatus.Load(container);
                if (lastStatus == null)
                {
                    lastStatus = new GageEventDetectorStatus();
                }
                sbDetails.Append("===================================\r\nlastStatus:\r\n");
                foreach (GageEventDetectorStatus.GageStatus s in lastStatus.Status)
                {
                    sbDetails.AppendFormat("{0}: {1}\r\n", s.Id, s.LastReadingId);
                }

                GageEventDetectorStatus currentStatus = new GageEventDetectorStatus();

                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();

                    Dictionary<int, List<SensorReading>> recentReadings = await SensorReading.GetAllRecentReadings(sqlcn, lastStatus.LastRunTime.AddHours(-RecentHours), false);

                    List<SensorLocationBase> locations = SensorLocationBase.GetLocations(sqlcn);
                    foreach (SensorLocationBase location in locations)
                    {
                        //$ TODO: Do we want to process 'deleted' gages anyway, and just mark events as "don't notify"?
                        if (location.IsDeleted)
                        {
                            continue;
                        }

                        GageEventDetectorStatus.GageStatus curStatus = lastStatus.Status.FirstOrDefault(s => s.Id == location.Id);
                        if (curStatus == null)
                        {
                            curStatus = new GageEventDetectorStatus.GageStatus(location.Id);
                        }
                        currentStatus.Status.Add(curStatus);

                        // If a gage is marked offline we explicitly skip it, per spec.
                        //$ TODO: Do we want to detect events anyway, and just mark them as "don't notify"?
                        if (location.IsOffline)
                        {
                            continue;
                        }

                        //$ TODO: Do we want to entirely ignore !IsActive and/or !IsPublic gages?

                        if (!recentReadings.ContainsKey(location.Id))
                        {
                            continue;
                        }
                        // Assume these come in most-recent-first
                        List<SensorReading> readings = recentReadings[location.Id];
                        if (readings.Count < 2)
                        {
                            continue;
                        }

                        if (readings[0].Id <= curStatus.LastReadingId)
                        {
                            continue;
                        }
                        Trends trends = TrendCalculator.CalculateWaterTrends(readings);
                        curStatus.LastReadingId = readings[0].Id;

                        location.ConvertValuesForDisplay();

                        // Theoretically, there could be more than one new reading per gage, but
                        // we intend to run this often enough that there won't be...
                        double curFeet = readings[0].WaterHeightFeet ?? 0;
                        double prevFeet = readings[1].WaterHeightFeet ?? 0;

                        // These threshold levels have a precedence -- if any two threshold levels are equal,
                        // we should detect events in this priority order.

                        //$ TODO: Add explicit check for RoadSaddleHeight?
                        if (location.Brown.HasValue)
                        {
                            if (await MaybeCreateEvent(sqlcn,
                                                       location,
                                                       readings[0],
                                                       readings[0].Timestamp,
                                                       curFeet,
                                                       prevFeet,
                                                       trends,
                                                       location.Brown.Value,
                                                       GageEventTypes.RedRising,
                                                       GageEventTypes.RedFalling))
                            {
                                continue;
                            }
                        }
                        if (location.Green.HasValue)
                        {
                            if (location.Brown.HasValue && location.Brown.Value == location.Green.Value)
                            {
                                continue;
                            }
                            if (await MaybeCreateEvent(sqlcn,
                                                       location,
                                                       readings[0],
                                                       readings[0].Timestamp,
                                                       curFeet,
                                                       prevFeet,
                                                       trends,
                                                       location.Green.Value,
                                                       GageEventTypes.YellowRising,
                                                       GageEventTypes.YellowFalling))
                            {
                                continue;
                            }
                        }
                    }

                    sbDetails.Append("===================================\r\ncurrentStatus:\r\n");
                    foreach (GageEventDetectorStatus.GageStatus s in currentStatus.Status)
                    {
                        sbDetails.AppendFormat("{0}: {1}\r\n", s.Id, s.LastReadingId);
                    }

#if SAVE_DETAILED_STATUS
                    await UploadStatusBlob(bcc, GageEventDetectorStatus.BlockName + "-details", sbDetails.ToString()
#endif
                    
                    await currentStatus.Save(container);

                    sqlcn.Close();
                    runLog.ReportJobRunSuccess();
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "GageEventDetector.DetectGageEvents", ex);
                runLog.ReportJobRunException(ex);
                throw;
            }
        }

        private static async Task<bool> MaybeCreateEvent(SqlConnection sqlcn,
                                                         SensorLocationBase location,
                                                         SensorReading newReading,
                                                         DateTime timestamp,
                                                         double curFeet,
                                                         double prevFeet,
                                                         Trends trends,
                                                         double threshold,
                                                         string risingType,
                                                         string fallingType)
        {
            GageEvent evt = null;
            
            if (curFeet >= threshold && prevFeet < threshold)
            {
                evt = new GageEvent()
                {
                    LocationId = location.Id,
                    EventType = risingType,
                    EventTime = timestamp,
                };
            }
            else if (curFeet < threshold && prevFeet >= threshold)
            {
                evt = new GageEvent()
                {
                    LocationId = location.Id,
                    EventType = fallingType,
                    EventTime = timestamp,
                };
            }

            if (evt != null)
            {
                DateTime? roadCrossing = null;

                if (trends != null && trends.TrendValue.HasValue && trends.TrendValue != 0)
                {
                    if (location.RoadSaddleHeight.HasValue)
                    {
                        if ((trends.TrendValue > 0 && location.RoadSaddleHeight.Value > curFeet) ||
                            (trends.TrendValue < 0 && location.RoadSaddleHeight.Value < curFeet))
                        {
                            double hours = (location.RoadSaddleHeight.Value - curFeet) / trends.TrendValue.Value;
                            roadCrossing = timestamp.AddHours(hours);
                        }
                    }
                }
                
                GageThresholdEventDetails details = new GageThresholdEventDetails()
                {
                    CurWaterLevel = curFeet,
                    PrevWaterLevel = prevFeet,
                    Trends = trends,
                    RoadCrossing = roadCrossing,
                    RoadSaddleHeight = location.RoadSaddleHeight,
                    Yellow = location.Green,
                    Red = location.Brown,
                    NewStatus = ApiLocationStatus.ComputeFloodLevel(newReading, location),
                };
                evt.GageThresholdEventDetails = details;
                
                await (evt.Save(sqlcn));
                return true;
            }

            return false;
        }
    }

    public class GageEventDetectorStatus
    {
        public const string BlockName = "GageEventDetector";
        public class GageStatus
        {
            public int Id               { get; set; }
            public int LastReadingId    { get; set; }

            public GageStatus()
            {
            }

            public GageStatus(int locationId)
            {
                this.Id = locationId;
            }
        }

        public DateTime LastRunTime     { get; set; }
        public List<GageStatus> Status  { get; set; }

        public GageEventDetectorStatus()
        {
            Status = new List<GageStatus>();
            LastRunTime = DateTime.UtcNow;
        }

        public GageStatus GetStatus(int locationId)
        {
            return Status.FirstOrDefault(gs => gs.Id == locationId);
        }

        public static async Task<GageEventDetectorStatus> Load(BlobContainerClient container)
        {
            return await FloodzillaAzureJob.LoadStatusBlob<GageEventDetectorStatus>(container, BlockName);
        }
        
        public async Task Save(BlobContainerClient container)
        {
            await FloodzillaAzureJob.SaveStatusBlob<GageEventDetectorStatus>(container, BlockName, this);
        }
    }
}
