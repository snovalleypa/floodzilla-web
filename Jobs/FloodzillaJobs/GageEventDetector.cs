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

namespace FloodzillaJobs
{
    public class GageEventDetector : FloodzillaJob
    {
        public GageEventDetector() : base("FloodzillaJob.DetectGageEvents",
                                          "Gage Event Detector")
        {
        }

        // If there's a gap bigger than this before the most recent gage reading, we
        // won't detect any threshold crossings...
        public const int RecentHours = 3;
        
        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            //$ TODO: Is there a better container to use for this kind of status info?
            BlobContainerClient container = await AzureJobHelpers.EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
            GageEventDetectorStatus lastStatus = await GageEventDetectorStatus.Load(container);
            if (lastStatus == null)
            {
                lastStatus = new GageEventDetectorStatus();
            }
#if DUMP_LAST_STATUS_FOR_DEBUGGING
            sbDetails.Append("===================================\r\nLast status:\r\n");
            foreach (GageEventDetectorStatus.GageStatus s in lastStatus.Status)
            {
                sbDetails.AppendFormat("{0}: {1}\r\n", s.Id, s.LastReadingId);
            }
#endif

            GageEventDetectorStatus currentStatus = new GageEventDetectorStatus();

            Dictionary<int, List<SensorReading>> recentReadings
                    = await SensorReading.GetAllRecentReadings(sqlcn, lastStatus.LastRunTime.AddHours(-RecentHours), false);
            List<SensorLocationBase> locations = SensorLocationBase.GetLocations(sqlcn);
            int locationCount = 0;
            int newReadingsCount = 0;
            int nearCount = 0;
            int floodingCount = 0;
            foreach (SensorLocationBase location in locations)
            {
                locationCount++;
                sbDetails.AppendFormat("Checking location {0} [{1}]--", location.Id, location.LocationName);
                //$ TODO: Do we want to process 'deleted' gages anyway, and just mark events as "don't notify"?
                if (location.IsDeleted)
                {
                    sbDetails.Append("deleted\r\n");
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
                    sbDetails.Append("offline\r\n");
                    continue;
                }

                //$ TODO: Do we want to entirely ignore !IsActive and/or !IsPublic gages?

                if (!recentReadings.ContainsKey(location.Id))
                {
                    sbDetails.Append("no recent readings\r\n");
                    continue;
                }

                // Assume these come in most-recent-first
                List<SensorReading> readings = recentReadings[location.Id];
                if (readings.Count < 2)
                {
                    sbDetails.Append("not enough recent readings\r\n");
                    continue;
                }

                if (readings[0].Id <= curStatus.LastReadingId)
                {
                    sbDetails.Append("no new readings\r\n");
                    continue;
                }
                newReadingsCount++;

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
                        floodingCount++;
                        sbDetails.Append("Flooding\r\n");
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
                        sbDetails.Append("Near Flooding\r\n");
                        nearCount++;
                        continue;
                    }
                }
                sbDetails.Append("\r\n");
            }

            sbSummary.AppendFormat("Checked {0} gages for events, {1} had new readings.  {2} near flooding, {3} flooding.",
                                   locationCount,
                                   newReadingsCount,
                                   nearCount,
                                   floodingCount);
            await currentStatus.Save(container);
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
            return await AzureJobHelpers.LoadStatusBlob<GageEventDetectorStatus>(container, BlockName);
        }
        
        public async Task Save(BlobContainerClient container)
        {
            await AzureJobHelpers.SaveStatusBlob<GageEventDetectorStatus>(container, BlockName, this);
        }
    }
}
