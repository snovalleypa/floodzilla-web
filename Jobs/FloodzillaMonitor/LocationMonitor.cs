using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;
using FzCommon.Processors;

namespace FloodZillaMonitor
{
    public class LocationMonitor
    {
        public static async Task MonitorLocations(ILogger log)
        {
            JobRunLog runLog = new JobRunLog("FloodZillaMonitor.MonitorLocations");

            try
            {
                BlobContainerClient container = await FloodzillaAzureJob.EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
                LocationMonitorStatus lastMonitorStatus = await LocationMonitorStatus.Load(container);
                if (lastMonitorStatus == null)
                {
                    lastMonitorStatus = new LocationMonitorStatus();
                }

                LocationMonitorStatus currentMonitorStatus = new LocationMonitorStatus() { LastRunTime = DateTime.UtcNow };

                StringBuilder sbDetails = new StringBuilder();
                StringBuilder sbOffline = new StringBuilder();

                List<SensorLocationBase> locations;
                int locCount = 0, checkedCount = 0, downCount = 0, notifyCount = 0, recoveredCount = 0;
                using (SqlConnection conn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    conn.Open();

                    //$ TODO: Do this for all regions
                    RegionBase region = RegionBase.GetRegion(conn, FzCommon.Constants.SvpaRegionId);
                    if (region.SensorOfflineThreshold == null)
                    {
                        throw new ApplicationException("Can't monitor locations -- Sensor Offline Threshold isn't set");
                    }

                    locations = SensorLocationBase.GetLocations(conn);
                    foreach (SensorLocationBase location in locations)
                    {
                        locCount++;
                        sbDetails.AppendFormat("Checking location {0} [{1}]--", location.Id, location.LocationName);
                        if (location.IsDeleted || !location.IsActive || !location.IsPublic)
                        {
                            sbDetails.Append("skipping");
                        }
                        else
                        {
                            checkedCount++;
                            List<SensorReading> lastReadings = await SensorReading.GetReadingsForLocation(location.Id, 1, null, null, 0);
                            if (lastReadings.Count == 0)
                            {
                                //$ TODO: Should this send email? 
                                sbDetails.Append("no readings");
                            }
                            else
                            {
                                LocationMonitorStatus.LocationStatus currentStatus = new LocationMonitorStatus.LocationStatus() { Id = location.Id };
                                LocationMonitorStatus.LocationStatus lastStatus = lastMonitorStatus.GetStatus(location.Id);
                                if (lastReadings[0].Timestamp.AddMinutes(region.SensorOfflineThreshold.Value) > currentMonitorStatus.LastRunTime)
                                {
                                    // Location is up.  If it was previously down, send all-clear notification.
                                    currentStatus.IsUp = true;

                                    if (lastStatus != null && !lastStatus.IsUp)
                                    {
                                        sbDetails.Append("RECOVERED");
                                        await NotifyRecovered(region, location, lastReadings[0], lastStatus.OfflineDetected);
                                        recoveredCount++;
                                    }
                                    else
                                    {
                                        sbDetails.Append("up");
                                    }
                                }
                                else
                                {
                                    // Location is down.  If we haven't previously sent error notification, send error notification.
                                    downCount++;
                                    currentStatus.IsUp = false;
                                    if (lastStatus == null || lastStatus.IsUp)
                                    {
                                        currentStatus.OfflineDetected = DateTime.UtcNow;
                                        sbDetails.Append("NOTIFYING DOWN");
                                        await NotifyDown(region, location, lastReadings[0]);
                                        notifyCount++;
                                    }
                                    else
                                    {
                                        if (lastStatus != null)
                                        {
                                            currentStatus.OfflineDetected = lastStatus.OfflineDetected;
                                        }
                                        sbDetails.AppendFormat("down (since {0})", currentStatus.OfflineDetected);
                                    }

                                    // Keep a list of currently-offline gags (for FzSlackBot, mainly)
                                    sbOffline.AppendFormat("OFFLINE: {0} - {1} (since {2})\r\n", location.PublicLocationId, location.LocationName, FzCommonUtility.ToRegionTimeFromUtc(currentStatus.OfflineDetected));
                                }
                                currentMonitorStatus.Status.Add(currentStatus);
                            }
                        }

                        sbDetails.Append("\r\n");
                    }

                    conn.Close();
                }

                string summary = String.Format("LocationMonitor @ {0}: {1} locations, checked {2}, down {3}, notify {4}, recovered {5}",
                                               FzCommonUtility.ToRegionTimeFromUtc(currentMonitorStatus.LastRunTime), locCount, checkedCount, downCount, notifyCount, recoveredCount);
                string offline = sbOffline.ToString();
                if (String.IsNullOrEmpty(offline))
                {
                    offline = "No sensors currently offline!";
                }

                await currentMonitorStatus.Save(container, offline, sbDetails.ToString(), summary);
                runLog.Summary = summary;
                runLog.ReportJobRunSuccess();
            }
            catch (Exception ex)
            {
                runLog.ReportJobRunException(ex);
                throw;
            }
        }

        private static async Task NotifyDown(RegionBase region, SensorLocationBase location, SensorReading lastReading)
        {
            if (!String.IsNullOrEmpty(region.NotifyList))
            {
                LocationDownEmailModel ldm = new LocationDownEmailModel()
                {
                    Region = region,
                    Location = location,
                    LastReading = lastReading,
                };
                await ldm.SendEmail(FzConfig.Config[FzConfig.Keys.EmailFromAddress], region.NotifyList);
            }

            string message = String.Format("GAGE OFFLINE: {0} - {1}; last reading {2}", location.PublicLocationId, location.LocationName, FzCommonUtility.ToRegionTimeFromUtc(lastReading.Timestamp));
            await SlackClient.NotifySlack(region.SlackNotifyUrl, message);
        }

        private static async Task NotifyRecovered(RegionBase region, SensorLocationBase location, SensorReading lastReading, DateTime offlineDetected)
        {
            if (!String.IsNullOrEmpty(region.NotifyList))
            {
                LocationRecoveredEmailModel lrm = new LocationRecoveredEmailModel()
                {
                    Region = region,
                    Location = location,
                    LastReading = lastReading,
                    OfflineDetected = offlineDetected,
                };
                await lrm.SendEmail(FzConfig.Config[FzConfig.Keys.EmailFromAddress], region.NotifyList);
            }

            string message = String.Format("GAGE RECOVERED: {0} - {1}; last reading {2} (was marked offline {3})", location.PublicLocationId, location.LocationName, FzCommonUtility.ToRegionTimeFromUtc(lastReading.Timestamp), FzCommonUtility.ToRegionTimeFromUtc(offlineDetected));
            await SlackClient.NotifySlack(region.SlackNotifyUrl, message);
        }
    }

    public class LocationMonitorStatus
    {
        public const string BlockName = "LocationMonitor";

        public class LocationStatus
        {
            public int Id;
            public bool IsUp;
            public DateTime OfflineDetected;
        }

        public DateTime LastRunTime;
        public List<LocationStatus> Status;
        
        public LocationMonitorStatus()
        {
            Status = new List<LocationStatus>();
        }

        public LocationStatus GetStatus(int locationId)
        {
            return Status.FirstOrDefault(ls => ls.Id == locationId);
        }

        public static async Task<LocationMonitorStatus> Load(BlobContainerClient container)
        {
            return await FloodzillaAzureJob.LoadStatusBlob<LocationMonitorStatus>(container, BlockName);
        }
        
        // The "details" and "summary" parameters are for inspection/debugging/etca
        public async Task Save(BlobContainerClient container, string offline, string details, string summary)
        {
            await FloodzillaAzureJob.SaveStatusBlob<LocationMonitorStatus>(container, BlockName, this);
            if (!String.IsNullOrEmpty(offline))
            {
                await FloodzillaAzureJob.SaveBlobText(container, BlockName + "-offline", offline);
            }
            if (!String.IsNullOrEmpty(details))
            {
                await FloodzillaAzureJob.SaveBlobText(container, BlockName + "-details", details);
            }
            if (!String.IsNullOrEmpty(summary))
            {
                await FloodzillaAzureJob.SaveBlobText(container, BlockName + "-summary", summary);
            }
        }
    }
}
