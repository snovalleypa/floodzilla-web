using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;
using FzCommon.Processors;

namespace FloodzillaJobs
{
    public class GageStatisticsCollector : FloodzillaJob
    {
        public GageStatisticsCollector() : base("FloodzillaJob.CollectGageStatistics",
                                                "Gage Statistics Collector")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            await this.CollectSenixGageStatistics(sqlcn, sbDetails, sbSummary);
                    
            //$ TODO: Collect stats for other gage types
        }

        public async Task CollectSenixGageStatistics(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);

            int locationCount = 0;
            int statsCount = 0;

            foreach (SensorLocationBase location in locations)
            {
                // If the location currently has a Senix device attached to it, then gather statistics for it.
                DeviceBase device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                if (device == null || device.DeviceTypeId != DeviceTypeIds.Senix)
                {
                    continue;
                }

                locationCount++;

                GageStatistics latest = await GageStatistics.GetLatestStatistics(sqlcn, location.Id);
                DateTime? firstDay = null;

                //$ TODO: Region
                if (latest != null)
                {
                    DateTime latestDateUtc = latest.Date;
                    DateTime latestDateRegion = FzCommonUtility.ToRegionTimeFromUtc(latestDateUtc);
                    latestDateRegion = latestDateRegion.AddDays(1);
                    firstDay = FzCommonUtility.ToUtcFromRegionTime(latestDateRegion);
                }

                sbDetails.AppendFormat("Checking location {0} [{1}]--", location.Id, location.LocationName);
                int processedReadings = 0;
                // If firstDay is null (because we don't yet have any stats), this will fetch all readings...
                List<SensorReading> allReadings
                        = await SensorReading.GetAllReadingsForLocation(location.Id,
                                                                        0,
                                                                        firstDay,
                                                                        null,
                                                                        0,
                                                                        0);
                if (allReadings == null || allReadings.Count == 0)
                {
                    sbDetails.Append("no readings");
                    continue;
                }

                // GetAllReadingsForLocation returns newest readings first, but for this it's easier to
                // go oldest-first.
                allReadings.Reverse();

                DateTime currentDayRegion = FzCommonUtility.ToRegionTimeFromUtc(allReadings[0].Timestamp).Date;
                int totalBattery = 0;
                double totalBatteryPct = 0;
                double totalRssi = 0;
                int expectedReadings = 0;
                int receivedReadings = 0;
                int readingCount = 0;

                int currentInterval = SenixSensorHelper.GetSampleRate(allReadings[0]);
                bool intervalHasChanged = false;

                // Start off at beginning-of-region-day minus our current interval
                DateTime lastReadingUtc = FzCommonUtility.ToUtcFromRegionTime(currentDayRegion).AddMinutes(-currentInterval);

                foreach (SensorReading sr in allReadings)
                {
                    processedReadings++;

                    // Some locations have had various kinds of devices attached.
                    DeviceBase thisDevice = devices.FirstOrDefault(d => d.DeviceId == sr.DeviceId);
                    if (thisDevice != null && thisDevice.DeviceTypeId != DeviceTypeIds.Senix)
                    {
                        continue;
                    }
                    
                    DateTime thisDayRegion = FzCommonUtility.ToRegionTimeFromUtc(sr.Timestamp).Date;
                    if (thisDayRegion > currentDayRegion)
                    {
                        if (readingCount > 0)
                        {
                            GageStatistics stats = new GageStatistics()
                            {
                                LocationId = location.Id,
                                Date = FzCommonUtility.ToUtcFromRegionTime(currentDayRegion),
                                AverageBatteryMillivolts = totalBattery / readingCount,
                                AverageBatteryPercent = totalBatteryPct / readingCount,
                                AverageRssi = totalRssi / (double)readingCount,
                                SensorUpdateInterval = currentInterval,
                                SensorUpdateIntervalChanged = intervalHasChanged,
                            };
                            if (expectedReadings > 0)
                            {
                                stats.PercentReadingsReceived = 100.0 * ((double)receivedReadings / (double)expectedReadings);
                            }
                            await stats.Save(sqlcn);
                            statsCount++;
                        }
                        
                        currentDayRegion = thisDayRegion;
                        totalBattery = 0;
                        totalBatteryPct = 0;
                        totalRssi = 0;
                        expectedReadings = 0;
                        receivedReadings = 0;
                        readingCount = 0;
                        intervalHasChanged = false;
                    }

                    dynamic senixData = SenixSensorHelper.GetRawData(sr);

                    // We need to double-check this because early versions of the senix listener did
                    // not properly ignore these readings.
                    string deleteReason;
                    if (SenixSensorHelper.ShouldIgnoreReading(senixData, null, out deleteReason))
                    {
                        continue;
                    }
                    
                    readingCount++;
                    receivedReadings++;
                    totalBattery += sr.BatteryVolt ?? 0;
                    totalBatteryPct += sr.BatteryPercent ?? 0;
                    totalRssi += sr.RSSI ?? 0;

                    int newInterval = SenixSensorHelper.GetSampleRate(senixData);
                    if (newInterval != currentInterval)
                    {
                        intervalHasChanged = true;
                        currentInterval = newInterval;
                    }
                    TimeSpan elapsed = sr.Timestamp - lastReadingUtc;

                    // Add some wiggle room to take clock skew into account
                    expectedReadings += ((int)(elapsed.TotalMinutes + 2)) / currentInterval;

                    lastReadingUtc = sr.Timestamp;
                    sbDetails.AppendFormat("Processed {0} readings", processedReadings);
                }

                // Don't worry about the last set of stats collected; we'll save it tomorrow.
                sbDetails.Append("\r\n");
            }
            sbSummary.AppendFormat("Saved {0} sets of statistics about {1} gages", statsCount, locationCount);
        }
    }
}