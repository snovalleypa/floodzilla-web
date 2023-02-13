using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;
using FzCommon.Processors;

namespace FloodzillaMonitor
{
    public class DailyStatusJob : FloodzillaJob
    {
        public DailyStatusJob() : base("FloodzillaMonitor.SendDailyStatus",
                                       "Daily Gage Status Summary Email")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            //$ TODO: Do this for all regions
            RegionBase region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
            if (String.IsNullOrEmpty(region.NotifyList))
            {
                throw new ApplicationException("Can't run daily status email -- region notify list is empty");
            }

            List<SensorLocationBase> locations = SensorLocationBase.GetLocations(sqlcn);
            List<DeviceBase> devices = DeviceBase.GetDevices(sqlcn);

            locations = locations.Where(l => l.IsActive && l.IsPublic && !l.IsDeleted)
                                 .Where(l => l.RegionId == region.RegionId)
                                 .OrderBy(l => l.Rank ?? 0)
                                 .ToList();

            DailyStatusEmailModel model = new DailyStatusEmailModel()
            {
                Region = region,
                Statuses = new List<DailyStatusEmailModel.LocationStatus>(),
            };
            foreach (SensorLocationBase location in locations)
            {
                DeviceBase device = devices.FirstOrDefault(d => (d.LocationId ?? 0) == location.Id);
                if (device == null)
                {
                    continue;
                }

                //$ TODO: Handle different types of devices besides Senix sensors
                if (device.DeviceTypeId != 4)
                {
                    continue;
                }

                location.ConvertValuesForDisplay();
                DailyStatusEmailModel.LocationStatus ls = new DailyStatusEmailModel.LocationStatus()
                {
                    LocationId = location.Id,
                    PublicLocationId = location.PublicLocationId,
                    LocationName = location.LocationName,
                    IsActive = location.IsActive,
                    IsPublic = location.IsPublic,
                    IsOffline = location.IsOffline,
                    GroundHeight = location.GroundHeight,
                    Green = location.Green,
                    Brown = location.Brown,
                    RoadSaddleHeight = location.RoadSaddleHeight,
                };

                List<SensorReading> lastReadings = await SensorReading.GetReadingsForLocation(location.Id, 1, null, null, 0);
                if (lastReadings.Count > 0)
                {
                    ls.LastUpdate = lastReadings[0].Timestamp;
                    ls.WaterLevel = (lastReadings[0].WaterHeight ?? 0.0) / 12.0;
                    ls.BatteryMillivolt = lastReadings[0].BatteryVolt ?? 0;
                    model.Statuses.Add(ls);
                }
            }

            List<GageStatistics> statsSummary = await GageStatistics.GetStatisticsSummary(sqlcn);
            model.LatestStatistics = new List<DailyStatusEmailModel.LocationStatistics>();
            foreach (GageStatistics stats in statsSummary)
            {
                SensorLocationBase loc = locations.FirstOrDefault(l => l.Id == stats.LocationId);
                if (loc == null)
                {
                    continue;
                }

                DailyStatusEmailModel.LocationStatistics lstat = new DailyStatusEmailModel.LocationStatistics();
                lstat.LocationId = stats.LocationId;
                lstat.LocationName = loc.LocationName;
                lstat.DateInRegionTime = region.ToRegionTimeFromUtc(stats.Date);
                lstat.Stats = stats;
                model.LatestStatistics.Add(lstat);
            }

            await model.SendEmail(FzConfig.Config[FzConfig.Keys.EmailFromAddress], region.NotifyList);
            sbSummary.AppendFormat("Sent daily status email about {0} locations to {1}", model.Statuses.Count, region.NotifyList);
        }
    }
}
