using System.Text;
using Microsoft.Data.SqlClient;

namespace FzCommon.Processors
{
    public class NoaaForecastProcessor
    {
        public static async Task<ForecastEmailModel> BuildEmailModel(SqlConnection sqlcn,
                                                                     NoaaForecastSet newForecasts,
                                                                     NoaaForecastSet previous)
        {
            //$ TODO: region
            RegionBase region = await RegionBase.GetRegionAsync(sqlcn, Constants.SvpaRegionId);

            // This is for testing.  If we pass in old forecasts, pretend they're current.
            DateTime now = DateTime.UtcNow;
            if ((now - newForecasts.Created).TotalMinutes > 60 * 24)
            {
                now = newForecasts.Created;
            }
            
            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
            List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
            List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);

            ForecastEmailModel model = new ForecastEmailModel();
            model.Region = region;
            bool hasFlooding = false;
            bool oldForecastHadFlooding = false;

            foreach (NoaaForecast newForecast in newForecasts.Forecasts)
            {
                // Don't worry about complaining if any of these tests fail; it won't
                // happen in normal usage, and even if it does happen we can't really do anything here...
                UsgsSite site = usgsSites.FirstOrDefault(u => u.NoaaSiteId == newForecast.NoaaSiteId);
                if (site == null)
                {
                    continue;
                }
                DeviceBase device = devices.FirstOrDefault(d => d.DeviceId == site.SiteId);
                if (device == null)
                {
                    continue;
                }
                SensorLocationBase location = locations.FirstOrDefault(l => l.Id == device.LocationId);
                if (location == null)
                {
                    continue;
                }

                if (!site.NotifyForecasts)
                {
                    continue;
                }

                if (location.DischargeStageOne.HasValue && newForecast.ExceedsDischargeThreshold(location.DischargeStageOne??0))
                {
                    hasFlooding = true;
                }

                // If the old forecast for this site had high water, remember that for below...
                NoaaForecast previousForecast = previous.GetForecast(newForecast.NoaaSiteId);
                if (previousForecast != null && location.DischargeStageOne.HasValue && previousForecast.ExceedsDischargeThreshold(location.DischargeStageOne??0))
                {
                    oldForecastHadFlooding = true;
                }

                ForecastEmailModel.ModelGageData mgd = new ForecastEmailModel.ModelGageData()
                {
                    GageId = location.PublicLocationId,
                    GageName = location.ShortName ?? location.LocationName,
                    GageShortName = location.ShortName,
                    GageRank = location.Rank ?? 0,
                    UsgsSiteId = site.SiteId,
                    Forecast = newForecast,
                    WarningCfsLevel = location.DischargeStageOne,
                };

                // Fetch readings for predictions/latest-reading display; also for last-24h-peak display
                mgd.Readings = await SensorReading.GetReadingsForLocation(location.Id,
                                                                          Trends.DesiredReadingCount,
                                                                          now.AddHours(-24),
                                                                          now);
                Trends trends = TrendCalculator.CalculateDischargeTrends(mgd.Readings);
                mgd.PredictedCfsPerHour = trends.TrendValue;

                model.GageForecasts.Add(mgd);
            }

            for (int iMeta = 0; iMeta < Metagages.MetagageIds.Length; iMeta++)
            {
                // If all of the metagage's components are available, add it to the list.
                string mgid = Metagages.MetagageSiteIds[iMeta];
                List<NoaaForecast> subForecasts = new List<NoaaForecast>();
                List<NoaaForecast> oldSubForecasts = new List<NoaaForecast>();
                List<Queue<SensorReading>> subReadings = new List<Queue<SensorReading>>();
                bool allFound = true;
                bool allOldFound = true;
                double rankSum = 0;
                int gageCount = 0;
                foreach (string gageid in mgid.Split('-'))
                {
                    NoaaForecast newForecast = newForecasts.GetForecast(gageid);
                    if (newForecast == null)
                    {
                        allFound = false;
                        break;
                    }
                    subForecasts.Add(newForecast);

                    NoaaForecast oldForecast = previous.GetForecast(gageid);
                    if (oldForecast != null)
                    {
                        oldSubForecasts.Add(oldForecast);
                    }
                    else
                    {
                        allOldFound = false;
                    }

                    UsgsSite site = usgsSites.FirstOrDefault(u => u.NoaaSiteId == gageid);
                    if (site == null)
                    {
                        allFound = false;
                        break;
                    }
                    DeviceBase device = devices.FirstOrDefault(d => d.DeviceId == site.SiteId);
                    if (device == null)
                    {
                        allFound = false;
                        break;
                    }
                    SensorLocationBase location = locations.FirstOrDefault(l => l.Id == device.LocationId);
                    if (location == null)
                    {
                        allFound = false;
                        break;
                    }
                    rankSum += location.Rank ?? 0;
                    gageCount++;

                    List<SensorReading> readings
                            = await SensorReading.GetReadingsForLocation(location.Id,
                                                                         Trends.DesiredReadingCount,
                                                                         now.AddHours(-24),
                                                                         now);
                    subReadings.Add(new Queue<SensorReading>(readings));
                }

                if (!allFound)
                {
                    continue;
                }

                // Check for high water in the previous metaforecast
                if (allOldFound)
                {
                    NoaaForecast oldSumForecast = NoaaForecast.SumForecasts(oldSubForecasts);
                    if (oldSumForecast != null)
                    {
                        if (oldSumForecast.ExceedsDischargeThreshold(Metagages.MetagageStageOnes[iMeta]))
                        {
                            oldForecastHadFlooding = true;
                        }
                    }
                }

                NoaaForecast sumForecast = NoaaForecast.SumForecasts(subForecasts);
                if (sumForecast != null)
                {
                    if (sumForecast.ExceedsDischargeThreshold(Metagages.MetagageStageOnes[iMeta]))
                    {
                        hasFlooding = true;
                    }
                    ForecastEmailModel.ModelGageData mgd = new ForecastEmailModel.ModelGageData()
                    {
                        GageId = Metagages.MetagageIds[iMeta],
                        GageName = Metagages.MetagageNames[iMeta],
                        GageShortName = Metagages.MetagageShortNames[iMeta],
                        GageRank = rankSum / gageCount,
                        Forecast = sumForecast,
                        WarningCfsLevel = Metagages.MetagageStageOnes[iMeta],
                    };
                    mgd.Readings = MetagageHelpers.SumReadings(subReadings);
                    model.GageForecasts.Add(mgd);
                }
            }

            model.GageForecasts.Sort((a, b) => (int)(a.GageRank - b.GageRank));

            model.HasFlooding = hasFlooding;
            model.OldForecastHadFlooding = oldForecastHadFlooding;
            return model;
        }

        //$ TODO: what details and status do we want from here? figure this out once we know what all it has to do
        public static async Task ProcessNewForecasts(SqlConnection sqlcn,
                                                     NoaaForecastSet newForecasts,
                                                     NoaaForecastSet previous,
                                                     StringBuilder sbDetails = null,
                                                     StringBuilder sbResult = null)
        {
            ForecastEmailModel model = await BuildEmailModel(sqlcn, newForecasts, previous);

            // For the every-forecast notification, we only want to do notifications if
            // (a) the current forecast shows flooding, or
            // (b) the current forecast is clear but the previous one showed flooding.
            if (model.HasFlooding || model.OldForecastHadFlooding)
            {
                await SlackClient.SendForecastNotification(model);

                model.Context = ForecastEmailModel.ForecastContext.Alert;
                List<UserBase> users = await UserBase.GetUsersForNotifyForecastAlerts(sqlcn);
                await model.SendEmailToUserList(sqlcn,
                                                FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                users,
                                                true,
                                                sbResult,
                                                sbDetails);
            }
        }
    }
}
