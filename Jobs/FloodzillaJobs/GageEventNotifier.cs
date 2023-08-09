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
    public class GageEventNotifier : FloodzillaJob
    {
        public GageEventNotifier() : base("FloodzillaJob.NotifyGageEvents",
                                          "Gage Event Notifier")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            BlobContainerClient container = await AzureJobHelpers.EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
            GageEventNotifierStatus lastStatus = await GageEventNotifierStatus.Load(container);
            if (lastStatus == null)
            {
                lastStatus = new GageEventNotifierStatus();
            }

            GageEventNotifierStatus currentStatus = new GageEventNotifierStatus();

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

                StringBuilder sbEmailResult = new StringBuilder();
                if (subs == null || subs.Count == 0)
                {
                    sbSummary.Append("No subscriptions...");
                }
                else
                {
                    List<UserBase> users = new List<UserBase>();
                    foreach (GageSubscription sub in subs)
                    {
                        UserBase user = UserBase.GetUser(sqlcn, sub.UserId);
                        users.Add(user);
                    }
                    await m_notificationManager.NotifyUserList(sqlcn,
                                                               model,
                                                               FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                               users,
                                                               true,
                                                               true,
                                                               true,
                                                               sbEmailResult,
                                                               sbDetails);
                }
                evt.NotifierProcessedTime = DateTime.UtcNow;
                evt.NotificationResult = sbEmailResult.ToString();
                await evt.Save(sqlcn);
            }
            sbSummary.AppendFormat("Events Processed: {0}", newEvents.Count);

            await currentStatus.Save(container);
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
            return await AzureJobHelpers.LoadStatusBlob<GageEventNotifierStatus>(container, BlockName);
        }
        
        public async Task Save(BlobContainerClient container)
        {
            await AzureJobHelpers.SaveStatusBlob<GageEventNotifierStatus>(container, BlockName, this);
        }
    }
}
