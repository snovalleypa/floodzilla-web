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
    public class GageEventNotifier : FloodzillaAzureJob
    {
        public static async Task NotifyGageEvents(ILogger log)
        {
            JobRunLog runLog = new JobRunLog("FloodzillaJob.NotifyGageEvents");

            try
            {
                BlobContainerClient container = await EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
                GageEventNotifierStatus lastStatus = await GageEventNotifierStatus.Load(container);
                if (lastStatus == null)
                {
                    lastStatus = new GageEventNotifierStatus();
                }

                GageEventNotifierStatus currentStatus = new GageEventNotifierStatus();

                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();

                    List<RegionBase> regions = await RegionBase.GetAllRegions(sqlcn);
                    List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);

                    List<GageEvent> newEvents = await GageEvent.GetUnprocessedEvents(sqlcn);
                    SmsClient client = new SmsClient();
                    foreach (GageEvent evt in newEvents)
                    {
                        SensorLocationBase location = locations.FirstOrDefault(l => l.Id == evt.LocationId);
                        if (location == null)
                        {
                            //$ TODO: this is bad, what to do about it
                            continue;
                        }
                        RegionBase region = regions.FirstOrDefault(r => r.RegionId == location.RegionId);
                        if (region == null)
                        {
                            //$ TODO: this is bad, what to do about it
                            continue;
                        }

                        GageEventEmailModel model = null;
                        switch (evt.EventType)
                        {
                            case GageEventTypes.RedRising:
                            case GageEventTypes.RedFalling:
                            case GageEventTypes.YellowRising:
                            case GageEventTypes.YellowFalling:
                            case GageEventTypes.RoadRising:
                            case GageEventTypes.RoadFalling:
                                model = BuildGageThresholdEventEmailModel(evt, region, location);
                                await SlackClient.SendGageThresholdEventNotification((GageThresholdEventEmailModel)model);
                                break;

                            case GageEventTypes.MarkedOffline:
                            case GageEventTypes.MarkedOnline:
                                model = BuildGageOnlineStatusEventEmailModel(evt, region, location);
                                break;
                        }
                        
                        List<GageSubscription> subs = await GageSubscription.GetSubscriptionsForGage(sqlcn, evt.LocationId);

                        StringBuilder sbResult = new StringBuilder();
                        StringBuilder sbDetails = new StringBuilder();
                        if (subs == null || subs.Count == 0)
                        {
                            sbResult.Append("No subscriptions");
                        }
                        else
                        {
                            List<UserBase> users = new List<UserBase>();
                            foreach (GageSubscription sub in subs)
                            {
                                UserBase user = UserBase.GetUser(sqlcn, sub.UserId);
                                users.Add(user);
                            }
                            await model.SendEmailToUserList(sqlcn,
                                                            FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                            users,
                                                            true,
                                                            sbResult,
                                                            sbDetails);
                        }
                        evt.NotifierProcessedTime = DateTime.UtcNow;
                        evt.NotificationResult = sbResult.ToString();
                        await evt.Save(sqlcn);
                    }

                    await currentStatus.Save(container);

                    sqlcn.Close();
                    runLog.Summary = String.Format("Events processed: {0}", newEvents.Count);
                    runLog.ReportJobRunSuccess();
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "GageEventNotifier.NotifyGageEvents", ex);
                runLog.ReportJobRunException(ex);
                throw;
            }
        }

        private static GageEventEmailModel BuildGageThresholdEventEmailModel(GageEvent evt, RegionBase region, SensorLocationBase location)
        {
            GageThresholdEventEmailModel model = new GageThresholdEventEmailModel()
            {
                GageEvent = evt,
                Region = region,
                Location = location,
            };
            return model;
        }

        private static GageEventEmailModel BuildGageOnlineStatusEventEmailModel(GageEvent evt, RegionBase region, SensorLocationBase location)
        {
            GageOnlineStatusEventEmailModel model = new GageOnlineStatusEventEmailModel()
            {
                GageEvent = evt,
                Region = region,
                Location = location,
            };
            return model;
        }
    }

    public class GageEventNotifierStatus
    {
        public const string BlockName = "GageEventNotifier";
        public DateTime LastRunTime     { get; set; }

        public GageEventNotifierStatus()
        {
            LastRunTime = DateTime.UtcNow;
        }

        public static async Task<GageEventNotifierStatus> Load(BlobContainerClient container)
        {
            return await FloodzillaAzureJob.LoadStatusBlob<GageEventNotifierStatus>(container, BlockName);
        }
        
        public async Task Save(BlobContainerClient container)
        {
            await FloodzillaAzureJob.SaveStatusBlob<GageEventNotifierStatus>(container, BlockName, this);
        }
    }
}
