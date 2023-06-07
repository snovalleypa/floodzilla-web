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

using System.IO;
using System.Xml.Serialization;

namespace FloodzillaJobs
{
    public class NoaaForecastFetcher : FloodzillaJob
    {
        public NoaaForecastFetcher() : base("FloodzillaJob.FetchNoaaForecasts",
                                            "NOAA Forecast Fetcher")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            BlobContainerClient container = await AzureJobHelpers.EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);

            int checkCount = 0;
            int noForecastCount = 0;
            int updatedCount = 0;

            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
            List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);

            NoaaForecastSet current = await NoaaForecastSet.GetLatestForecastSet(sqlcn);
            NoaaForecastSet previous = new NoaaForecastSet();
            previous.Created = current.Created;
            NoaaForecastSet newForecasts = new NoaaForecastSet();
            newForecasts.Created = DateTime.UtcNow;

            List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);
            foreach (UsgsSite site in usgsSites)
            {
                checkCount++;
                sbDetails.AppendFormat("Checking site {0}...", site.NoaaSiteId);
                NoaaForecast newForecast = await NoaaForecast.FetchNewForecast(site.NoaaSiteId);
                if (newForecast == null)
                {
                    sbDetails.AppendFormat("no forecast.\r\n");
                    noForecastCount++;
                    continue;
                }
                sbDetails.AppendFormat("Forecast date: {0:g}...", newForecast.Created);
                NoaaForecast lastForecast = current.GetForecast(site.NoaaSiteId);
                if (lastForecast != null)
                {
                    sbDetails.AppendFormat("Last forecast: {0:g}...", lastForecast.Created);
                    if (newForecast.ForecastHasChanged(lastForecast))
                    {
                        previous.Forecasts.Add(lastForecast);
                    }
                    else
                    {
                        sbDetails.Append("Skipping\r\n");
                        continue;
                    }
                }

                // For peak calculation, we want to snapshot the current water height/discharge; this way
                // we can identify cases where the first forecast value represets a peak.
                DeviceBase device = devices.FirstOrDefault(d => d.DeviceId == site.SiteId);
                if (device != null)
                {
                    SensorLocationBase location = locations.FirstOrDefault(l => l.Id == device.LocationId);
                    if (location != null)
                    {
                        List<SensorReading> currentValues
                            = await SensorReading.GetReadingsForLocation(location.Id,
                                                                         1,
                                                                         DateTime.UtcNow.AddHours(-1),
                                                                         DateTime.UtcNow);
                        if (currentValues != null && currentValues.Count > 0)
                        {
                            SensorReading currentReading = currentValues[0];
                            newForecast.CurrentWaterHeight = currentReading.WaterHeightFeet;
                            newForecast.CurrentDischarge = currentReading.WaterDischarge;
                        }
                    }
                }

                newForecasts.Forecasts.Add(newForecast);
                updatedCount++;
                sbDetails.Append("Saving\r\n");
                await newForecast.Save(sqlcn);
            }

            await AzureJobHelpers.SaveBlobText(container, "noaa-forecast-fetcher-details", sbDetails.ToString());

            if (updatedCount > 0)
            {
                NoaaForecastProcessorJob processor = new NoaaForecastProcessorJob(previous, newForecasts);
                await processor.Execute();
            }
            sbSummary.AppendFormat("Checked {0} sites. {1} had no forecast. {2} were updated.", checkCount, noForecastCount, updatedCount);
        }
    }

    public class NoaaForecastProcessorJob : FloodzillaJob
    {
        public NoaaForecastProcessorJob(NoaaForecastSet previousForecasts,
                                        NoaaForecastSet newForecasts)
                : base("FloodzillaJob.ProcessNewForecasts",
                       "NOAA Forecast Processor")
        {
            this.m_previousForecasts = previousForecasts;
            this.m_newForecasts = newForecasts;
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            await NoaaForecastProcessor.ProcessNewForecasts(sqlcn,
                                                            this.m_notificationManager,
                                                            this.m_newForecasts,
                                                            this.m_previousForecasts,
                                                            sbDetails,
                                                            sbSummary);
        }

        private NoaaForecastSet m_previousForecasts;
        private NoaaForecastSet m_newForecasts;
    }
}
