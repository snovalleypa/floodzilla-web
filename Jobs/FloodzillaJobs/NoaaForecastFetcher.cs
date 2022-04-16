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

namespace FloodzillaJob
{
    public class NoaaForecastFetcher : FloodzillaAzureJob
    {
        public static async Task FetchNoaaForecasts(ILogger log)
        {
            JobRunLog runLog = new JobRunLog("FloodzillaJob.FetchNoaaForecasts");
            StringBuilder sbDetails = new StringBuilder();

            try
            {
                BlobContainerClient container = await EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);

                int checkCount = 0;
                int noForecastCount = 0;
                int updatedCount = 0;

                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();

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

                    await SaveBlobText(container, "noaa-forecast-fetcher-details", sbDetails.ToString());

                    if (updatedCount > 0)
                    {
                        await ProcessNewForecasts(sqlcn, container, newForecasts, previous);
                    }

                    sqlcn.Close();
                    runLog.Summary = String.Format("Checked {0} sites. {1} had no forecast. {2} were updated.", checkCount, noForecastCount, updatedCount);
                    runLog.ReportJobRunSuccess();
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "NoaaForecastFetcher.FetchNoaaForecasts", ex);
                runLog.ReportJobRunException(ex);
                throw;
            }
        }

        public static async Task ProcessNewForecasts(SqlConnection sqlcn,
                                                     BlobContainerClient container,
                                                     NoaaForecastSet newForecasts,
                                                     NoaaForecastSet previousForecasts)
        {
            JobRunLog runLog = new JobRunLog("FloodzillaJob.ProcessNewForecasts");
            StringBuilder sbDetails = new StringBuilder();
            StringBuilder sbResult = new StringBuilder();

            try
            {
                await NoaaForecastProcessor.ProcessNewForecasts(sqlcn, newForecasts, previousForecasts, sbDetails, sbResult);

                await SaveBlobText(container, "noaa-forecast-processor-details", sbDetails.ToString());
                
                runLog.Summary = sbResult.ToString();
                runLog.ReportJobRunSuccess();
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "NoaaForecastFetcher.ProcessNewForecasts", ex);
                runLog.ReportJobRunException(ex);
                throw;
            }
        }
    }
}
