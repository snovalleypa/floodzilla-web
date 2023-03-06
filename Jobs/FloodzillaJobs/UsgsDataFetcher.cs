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
    public class UsgsDataFetcher : FloodzillaJob
    {
        public UsgsDataFetcher() : base("FloodzillaJob.FetchUsgsReadings",
                                        "USGS Reading Fetcher")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            string listenerInfo = "FloodzillaJob.FetchUsgsReadings, 2/2023, " + Environment.MachineName;

            int errorCount = 0;
            int deviceCount = 0;
            int readingCount = 0;

            //$ TODO: Region?
            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
            List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);
            List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);

            foreach (DeviceBase device in devices)
            {
                //$ TODO: Generalize this per device-type
                if (device.DeviceTypeId == DeviceTypeIds.Usgs)
                {
                    deviceCount++;
                    if (!device.UsgsSiteId.HasValue)
                    {
                        //$ TODO: Do we want to report this error? It'll happen every five minutes
                        //$ until it's resolved, so email's not a great choice...
                        errorCount++;
                        sbDetails.AppendFormat("Device {0}: Missing USGS Site id\r\n", device.DeviceId);
                        continue;
                    }

                    UsgsSite usgsSite = usgsSites.FirstOrDefault(u => u.SiteId == device.UsgsSiteId);
                    if (usgsSite == null)
                    {
                        //$ TODO: Do we want to report this error? It'll happen every five minutes
                        //$ until it's resolved, so email's not a great choice...
                        errorCount++;
                        sbDetails.AppendFormat("Device {0}: USGS Site ID {1} does not exist\r\n", device.DeviceId, device.UsgsSiteId);
                        continue;
                    }

                    SensorLocationBase location = locations.FirstOrDefault(l => l.Id == device.LocationId);
                    if (location == null)
                    {
                        // This isn't an error per se, because devices don't have to be associated
                        // with locations, but we might still want to report it someday?
                        errorCount++;
                        sbDetails.AppendFormat("Device {0}: Location ID {1} does not exist\r\n", device.DeviceId, device.LocationId);
                        continue;
                    }

                    if (!device.IsActive)
                    {
                        errorCount++;
                        sbDetails.AppendFormat("Device {0}: Skipping inactive device\r\n", device.DeviceId);
                        continue;
                    }
                    if (location.IsOffline)
                    {
                        errorCount++;
                        sbDetails.AppendFormat("Device {0}: Skipping offline location {1}\r\n", device.DeviceId, device.LocationId);
                        continue;
                    }

                    List<SensorReading> lastReadings
                            = await SensorReading.GetReadingsForDevice(device.DeviceId,
                                                                       1,
                                                                       null,
                                                                       null);
                    DateTime startDate = DateTime.UtcNow.AddDays(-7);
                    if (lastReadings != null && lastReadings.Count > 0)
                    {
                        if (lastReadings[0].Timestamp > startDate)
                        {
                            startDate = DateTime.SpecifyKind(lastReadings[0].Timestamp, DateTimeKind.Utc);
                        }
                    }

                    UsgsSite.UsgsReadingData readingData = await usgsSite.FetchUsgsReadings(listenerInfo, startDate, device, location);
                    if (readingData == null)
                    {
                        // Any major errors will have been reported by FetchUsgsReadings()...
                        errorCount++;
                        sbDetails.AppendFormat("Device {0}, Location ID {1}: unable to fetch USGS readings\r\n", device.DeviceId, device.LocationId);
                        continue;
                    }
                    if (readingData.Readings == null || readingData.Readings.Count == 0)
                    {
                        // This doesn't count as an error.
                        sbDetails.AppendFormat("Device {0}, Location ID {1}: Fetched zero USGS readings\r\n", device.DeviceId, device.LocationId);
                        continue;
                    }

                    sbDetails.AppendFormat("Device {0}, Location ID {1}: Fetched {2} USGS readings\r\n", device.DeviceId, device.LocationId, readingData.Readings.Count);

                    // Readings are in forward chronological order.  If we had a previous reading, the
                    // first reading we receive should be the same, so we should skip it.
                    int firstReading = 0;
                    if (lastReadings != null && lastReadings.Count > 0)
                    {
                        if (readingData.Readings[0].Timestamp == lastReadings[0].Timestamp)
                        {
                            firstReading = 1;
                        }
                    }

                    for (int i = firstReading; i < readingData.Readings.Count; i++)
                    {
                        readingCount++;
                        sbDetails.AppendFormat("Device {0}, location ID {1}: Received new reading at {2:O}\r\n",
                                               device.DeviceId,
                                               device.LocationId,
                                               readingData.Readings[i].Timestamp);
                        await readingData.Readings[i].Save(sqlcn);
                    }

                    // If we got any new data, go ahead and save the USGS metadata as well.
                    if (readingData.Readings.Count > firstReading)
                    {
                        await SensorLocationBase.UpdateLocationLatLong(sqlcn, device.LocationId.Value, readingData.Latitude, readingData.Longitude);

                        //$ TODO: Do we care about saving the USGS Site Name? It's all-caps and
                        //$ not especially user-friendly...
                    }
                }
                
            }
            sbSummary.AppendFormat("Checked {0} sites. {1} had errors. Received {2} new readings",
                                   deviceCount, errorCount, readingCount);
        }
    }
}
