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
using System.Runtime.CompilerServices;

namespace FloodzillaMonitor
{
    public class LocationMonitor : FloodzillaJob
    {
        public LocationMonitor() : base("FloodzillaMonitor.MonitorLocations",
                                        "Gage Status Monitor")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            BlobContainerClient container = await AzureJobHelpers.EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
            LocationMonitorStatus lastMonitorStatus = await LocationMonitorStatus.Load(container);
            if (lastMonitorStatus == null)
            {
                lastMonitorStatus = new LocationMonitorStatus();
            }

            LocationMonitorStatus currentMonitorStatus = new LocationMonitorStatus() { LastRunTime = DateTime.UtcNow };

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
                                    await this.NotifyRecovered(sqlcn, region, location, lastReadings[0], lastStatus.OfflineDetected);
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
                                    await this.NotifyDown(sqlcn, region, location, lastReadings[0]);
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

            sbSummary.AppendFormat("LocationMonitor @ {0}: {1} locations, checked {2}, down {3}, notify {4}, recovered {5}",
                                   FzCommonUtility.ToRegionTimeFromUtc(currentMonitorStatus.LastRunTime),
                                   locCount,
                                   checkedCount,
                                   downCount,
                                   notifyCount,
                                   recoveredCount);
            string offline = sbOffline.ToString();
            if (String.IsNullOrEmpty(offline))
            {
                offline = "No sensors currently offline!";
            }

            await currentMonitorStatus.Save(container, offline, sbDetails.ToString(), sbSummary.ToString());
        }

        private async Task NotifyDown(SqlConnection sqlcn, RegionBase region, SensorLocationBase location, SensorReading lastReading)
        {
            if (!String.IsNullOrEmpty(region.NotifyList))
            {
                LocationDownEmailModel ldm = new LocationDownEmailModel()
                {
                    Region = region,
                    Location = location,
                    LastReading = lastReading,
                };
                await m_notificationManager.SendEmailModelToRecipientList(sqlcn,
                                                                          ldm,
                                                                          FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                                          region.NotifyList);
            }

            string message = String.Format("GAGE OFFLINE: {0} - {1}; last reading {2}", location.PublicLocationId, location.LocationName, FzCommonUtility.ToRegionTimeFromUtc(lastReading.Timestamp));
            await SlackClient.NotifySlack(region.SlackNotifyUrl, message);
        }

        private async Task NotifyRecovered(SqlConnection sqlcn, RegionBase region, SensorLocationBase location, SensorReading lastReading, DateTime offlineDetected)
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
                await m_notificationManager.SendEmailModelToRecipientList(sqlcn,
                                                                          lrm,
                                                                          FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                                          region.NotifyList);
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
            return await AzureJobHelpers.LoadStatusBlob<LocationMonitorStatus>(container, BlockName);
        }

        public async Task Save(BlobContainerClient container, string offline, string details, string summary)
        {
            await AzureJobHelpers.SaveStatusBlob<LocationMonitorStatus>(container, BlockName, this);
            if (!String.IsNullOrEmpty(offline))
            {
                await AzureJobHelpers.SaveBlobText(container, BlockName + "-offline", offline);
            }
            if (!String.IsNullOrEmpty(details))
            {
                await AzureJobHelpers.SaveBlobText(container, BlockName + "-details", details);
            }
            if (!String.IsNullOrEmpty(summary))
            {
                await AzureJobHelpers.SaveBlobText(container, BlockName + "-summary", summary);
            }
        }
    }
}
