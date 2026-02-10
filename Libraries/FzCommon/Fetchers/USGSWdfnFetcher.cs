using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace FzCommon.Fetchers
{
    public class USGSWdfnFetcher
    {
        // This is the furthest back we will go to fill in data for this gauge; if it's not a new gauge
        // but we don't have any data newer than this, we'll leave a gap...
        const int MAX_BACKFILL_DAYS = 7;

        // This is used to indicate that the USGS system does not have data for this reading.  We need
        // to be able to record "we got data back and it is invalid" and I don't want to introduce a
        // secondary field in the reading, so we're using a sentinel.  We already have a situation where
        // the old USGS API was returning values of -999999 when their equipment was offline, so we'll
        // continue to use that here.
        const double INVALID_READING_SENTINEL = -999999.0;

        public class UpdateStatus
        {
            public UpdateStatus(StringBuilder sbDetails, StringBuilder sbSummary)
            {
                this.sbDetails = sbDetails;
                this.sbSummary = sbSummary;
            }
            public readonly StringBuilder sbDetails;
            public readonly StringBuilder sbSummary;
            public int errorCount;
            public int heightReadingCount;
            public int dischargeReadingCount;
        }

        public async static Task UpdateAllUsgsGauges(SqlConnection sqlcn,
                                                     StringBuilder sbDetails,
                                                     StringBuilder sbSummary,
                                                     bool forceTestMode = false)
        {
            string listenerInfo = "USGSWdfnFetcher, 1/2026, " + Environment.MachineName;

            UpdateStatus updStat = new(sbDetails, sbSummary)
            {
                errorCount = 0,
                heightReadingCount = 0,
                dischargeReadingCount = 0,
            };
            int deviceCount = 0;

            int targetDeviceType = DeviceTypeIds.Usgs;
            bool testOnlyMode = false;
            if (forceTestMode || FzConfig.Config[FzConfig.Keys.UsgsFetcherTestOnlyMode] == "true")
            {
                testOnlyMode = true;
                targetDeviceType = DeviceTypeIds.UsgsTestingDevice;
            }
            if (testOnlyMode)
            {
                listenerInfo = "[TEST MODE] " + listenerInfo;
            }

            //$ TODO: Region?  Probably not, this should probably always run for all USGS sites in all regions,
            //$ unless there's a reason not to (different schedules per region, maybe?)
            List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
            List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);
            List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);

            foreach (DeviceBase device in devices)
            {
                if (device.IsDeleted)
                {
                    continue;
                }
                if (device.DeviceTypeId == targetDeviceType)
                {
                    deviceCount++;
                    if (!device.UsgsSiteId.HasValue)
                    {
                        //$ TODO: Do we want to report this error? It'll happen every five minutes
                        //$ until it's resolved, so email's not a great choice...
                        updStat.errorCount++;
                        updStat.sbDetails.AppendFormat("Device {0}: Missing USGS Site id\r\n", device.DeviceId);
                        continue;
                    }
                    if (!device.LocationId.HasValue)
                    {
                        updStat.errorCount++;
                        updStat.sbDetails.AppendFormat("Device {0} has no associated location", device.DeviceId);
                        continue;
                    }
                    int locationId = device.LocationId.Value;

                    UsgsSite? usgsSite = usgsSites.FirstOrDefault(u => u.SiteId == device.UsgsSiteId);
                    if (usgsSite == null)
                    {
                        //$ TODO: Do we want to report this error? It'll happen every five minutes
                        //$ until it's resolved, so email's not a great choice...
                        updStat.errorCount++;
                        updStat.sbDetails.AppendFormat("Device {0}: USGS Site ID {1} does not exist\r\n", device.DeviceId, device.UsgsSiteId);
                        continue;
                    }

                    SensorLocationBase? location = locations.FirstOrDefault(l => l.Id == locationId);
                    if (location == null)
                    {
                        // This isn't an error per se, because devices don't have to be associated
                        // with locations, but we might still want to report it someday?
                        updStat.errorCount++;
                        updStat.sbDetails.AppendFormat("Device {0}: Location ID {1} does not exist\r\n", device.DeviceId, locationId);
                        continue;
                    }

                    if (!testOnlyMode)
                    {
                        if (!device.IsActive)
                        {
                            updStat.errorCount++;
                            updStat.sbDetails.AppendFormat("Device {0}: Skipping inactive device\r\n", device.DeviceId);
                            continue;
                        }
                        if (location.IsOffline)
                        {
                            updStat.errorCount++;
                            updStat.sbDetails.AppendFormat("Device {0}: Skipping offline location {1}\r\n", device.DeviceId, locationId);
                            continue;
                        }
                    }

                    await UpdateUsgsGauge(sqlcn,
                                          usgsSite,
                                          device,
                                          location,
                                          listenerInfo,
                                          updStat);

                }
            }
            sbSummary.AppendFormat("Checked {0} sites. {1} had errors. Received {2} new height readings, {3} new discharge readings",
                                   deviceCount,
                                   updStat.errorCount,
                                   updStat.heightReadingCount,
                                   updStat.dischargeReadingCount);
        }

        // This is mostly intended for use by tools which are re-checking existing data.
        public async static Task UpdateUsgsGaugeForDateRange(SqlConnection sqlcn,
                                                             UsgsSite usgsSite,
                                                             DeviceBase device,
                                                             SensorLocationBase location,
                                                             DateTime startTimeUtc,
                                                             DateTime endTimeUtc,
                                                             string listenerInfo,
                                                             UpdateStatus updStat)
        {
            List<SensorReading> availableReadings = await SensorReading.GetAllReadingsForLocation(location.Id, 0, startTimeUtc, endTimeUtc);
            await FetchAndUpdateGaugeData(sqlcn,
                                          usgsSite,
                                          device,
                                          location,
                                          startTimeUtc,
                                          endTimeUtc,
                                          availableReadings,
                                          listenerInfo,
                                          updStat);
        }

        // NOTE: Be careful with this -- it's exposed here primarily for testing, but it's generally
        // meant to be called from UpdateAllUsgsGauges.
        public async static Task UpdateUsgsGauge(SqlConnection sqlcn,
                                                 UsgsSite usgsSite,
                                                 DeviceBase device,
                                                 SensorLocationBase location,
                                                 string listenerInfo,
                                                 UpdateStatus updStat)
        {
            // First, determine how far back we need to go.  Since we may have gotten readings with just
            // discharge or just water height, we need to figure out the latest reading for each type of data.
            SensorReading? latestHeight = await SensorReading.GetLatestReceivedWaterHeightSensorReadingForLocation(location.Id);
            SensorReading? latestDischarge = null;
            if (usgsSite.NoDischarge)
            {
                latestDischarge = latestHeight;
            }
            else
            {
                latestDischarge = await SensorReading.GetLatestReceivedDischargeSensorReadingForLocation(location.Id);
            }
            DateTime startTime = DateTime.MaxValue;
            DateTime earliestStart = DateTime.UtcNow - TimeSpan.FromDays(MAX_BACKFILL_DAYS);
            if (latestDischarge != null)
            {
                if (latestDischarge.Timestamp < startTime)
                {
                    startTime = latestDischarge.Timestamp;
                }
            }
            if (latestHeight != null)
            {
                if (latestHeight.Timestamp < startTime)
                {
                    startTime = latestHeight.Timestamp;
                }
            }
            if (startTime == DateTime.MaxValue || startTime < earliestStart)
            {
                startTime = earliestStart;
            }
            if (startTime.Kind == DateTimeKind.Unspecified)
            {
                // Coerce this to DateTimeKind.Utc if it came out of the database layer as Unspecified.
                startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second, DateTimeKind.Utc);
            }

            List<SensorReading> availableReadings = new();
            // This is a shortcut -- if our latest discharge reading and our latest height reading are the same
            // (which should be the common case, if we're updating a gauge that has been receiving data), we don't
            // need to go back and fetch any old data, we can just start with the reading we've got.
            if (latestDischarge != null && latestHeight != null && latestDischarge.Timestamp == latestHeight.Timestamp)
            {
                if (latestDischarge.Id != latestHeight.Id)
                {
                    ErrorManager.ReportError(ErrorSeverity.Major,
                                             "FloodzillaJobs.UsgsDataFetcher",
                                             String.Format("Duplicate reading for location {0}, timestamp {1}: {2} != {3}", location.Id, latestDischarge.Timestamp, latestDischarge.Id, latestHeight.Id));
                }
                availableReadings.Add(latestHeight);
            }
            else
            {
                // We need to make sure we've loaded every relevant reading.
                availableReadings = await SensorReading.GetAllReadingsForLocation(location.Id, 0, startTime, DateTime.UtcNow);
            }
            await FetchAndUpdateGaugeData(sqlcn,
                                          usgsSite,
                                          device,
                                          location,
                                          startTime,
                                          DateTime.UtcNow,
                                          availableReadings,
                                          listenerInfo,
                                          updStat);
        }

        private async static Task FetchAndUpdateGaugeData(SqlConnection sqlcn,
                                                          UsgsSite usgsSite,
                                                          DeviceBase device,
                                                          SensorLocationBase location,
                                                          DateTime startTimeUtc,
                                                          DateTime endTimeUtc,
                                                          List<SensorReading> availableReadings,
                                                          string listenerInfo,
                                                          UpdateStatus updStat)
        {


            // Now we need to ask for new data.  These will return null if there was an error fetching the data;
            // if we can't do the first fetch it seems like we shouldn't bother doing the next fetch.  Also: we
            // fetch discharge first because it minimizes the chances of getting a reading with just discharge
            // but no height, which can potentially cause problems later.
            List<WdfnReading>? dischargeReadings = null;
            List<WdfnReading>? heightReadings = null;
            if (usgsSite.NoDischarge)
            {
                dischargeReadings = new();
            }
            else
            {
                dischargeReadings = await usgsSite.FetchRawWdfnReadings(startTimeUtc, endTimeUtc, device, WdfnReadingType.Discharge);;
            }
            if (dischargeReadings != null)
            {
                heightReadings = await usgsSite.FetchRawWdfnReadings(startTimeUtc, endTimeUtc, device, WdfnReadingType.Height);
            }
            if (heightReadings == null || dischargeReadings == null)
            {
                // The fetcher code should have reported any errors; we just remember that we had an error here and move on.
                updStat.errorCount++;
                updStat.sbDetails.AppendFormat("Device {0}, Location ID {1}: unable to fetch WDFN USGS readings\r\n", device.DeviceId, device.LocationId);
                return;
            }
            if (heightReadings.Count == 0 && dischargeReadings.Count == 0)
            {
                // No news is good news.
                updStat.sbDetails.AppendFormat("Device {0}: No new USGS data available", device.DeviceId);
                return;
            }

            // Keep track of which, if any, of our already-loaded readings will have to be saved.
            List<bool> dirty = new(availableReadings.Count);
            for (int i = 0; i < availableReadings.Count; i++)
            {
                dirty.Add(false);
            }

            UpdateStatus subStat = new(updStat.sbDetails, updStat.sbSummary)
            {
                errorCount = 0,
                heightReadingCount = 0,
                dischargeReadingCount = 0,
            };

            UpdateReadings(availableReadings, dirty, heightReadings, WdfnReadingType.Height, UpdateReadingWaterHeight, location, device, listenerInfo, subStat);
            UpdateReadings(availableReadings, dirty, dischargeReadings, WdfnReadingType.Discharge, UpdateReadingDischarge, location, device, listenerInfo, subStat);

            for (int i = 0; i < dirty.Count; i++)
            {
                if (dirty[i])
                {
                    if (availableReadings[i].WaterDischarge < 0 || availableReadings[i].WaterHeight < 0)
                    {
                        availableReadings[i].IsDeleted = true;
                        availableReadings[i].IsFiltered = true;
                        availableReadings[i].DeleteReason = (availableReadings[i].WaterDischarge < 0)
                                               ? "Filtered (invalid water discharge)"
                                               : "Filtered (invalid water height)";
                    }
                    await availableReadings[i].Save(sqlcn);
                }
            }
            updStat.sbDetails.AppendFormat("Device {0}: {1} new height readings, {2} new discharge readings\n",
                                           device.DeviceId,
                                           subStat.heightReadingCount,
                                           subStat.dischargeReadingCount);
            updStat.sbSummary.AppendFormat("Device {0}: {1} new height readings, {2} new discharge readings\n",
                                           device.DeviceId,
                                           subStat.heightReadingCount,
                                           subStat.dischargeReadingCount);
            updStat.errorCount += subStat.errorCount;
            updStat.heightReadingCount += subStat.heightReadingCount;
            updStat.dischargeReadingCount += subStat.dischargeReadingCount;
        }

        private static void UpdateReadings(List<SensorReading> availableReadings,
                                           List<bool> dirty,
                                           List<WdfnReading> readings,
                                           WdfnReadingType readingType,
                                           Func<SensorReading, DeviceBase, SensorLocationBase, double?, UpdateStatus, bool> updater,
                                           SensorLocationBase location,
                                           DeviceBase device,
                                           string listenerInfo,
                                           UpdateStatus updStat)
        {
            foreach (WdfnReading reading in readings)
            {
                int readingIndex = EnsureSensorReadingForTimestamp(availableReadings,
                                                                   dirty,
                                                                   reading.Timestamp,
                                                                   listenerInfo,
                                                                   location.Id,
                                                                   device);
                if (updater(availableReadings[readingIndex], device, location, reading.Value, updStat))
                {
                    dirty[readingIndex] = true;
                }
            }
        }

        private static bool UpdateReadingWaterHeight(SensorReading reading,
                                                     DeviceBase device,
                                                     SensorLocationBase location,
                                                     double? newWaterHeightFeet,
                                                     UpdateStatus updStat)
        {
            bool dirty = false;
            if (!newWaterHeightFeet.HasValue)
            {
                if (reading.WaterHeightFeet.HasValue)
                {
                    // Clear out the water height and all associated fields; apparently a previously-valid-looking
                    // data point was discarded by USGS (this happened in late December 2025 on the USGS
                    // Carnation gauge, which apparently had some kind of failure).  NOTE: We are using a
                    // sentinel here to indicate invalid data because we need to be able to distinguish between
                    // "We haven't received data yet" and "We have received an indication that USGS has no data".
                    // ALSO NOTE: we do this in kind of a brute-force way so that all possible relevant fields are marked.
                    reading.WaterHeight = INVALID_READING_SENTINEL;
                    reading.WaterHeightFeet = INVALID_READING_SENTINEL;
                    reading.DistanceReading = 0 - INVALID_READING_SENTINEL;         // (this is kind of bad, but it matches what existing data does)
                    reading.DistanceReadingFeet = 0 - INVALID_READING_SENTINEL;         // (this is kind of bad, but it matches what existing data does)
                    reading.RawWaterHeight = INVALID_READING_SENTINEL;
                    reading.RawWaterHeightFeet = INVALID_READING_SENTINEL;
                    updStat.sbDetails.AppendFormat("Device {0}: Removing height reading at {1}\n", device.DeviceId, reading.Timestamp);
                    dirty = true;
                    updStat.heightReadingCount++;
                }
            }
            else
            {
                // The location has not been converted for display, so its internal values are in inches, which
                // is what we need to put into the reading.  Sigh.
                double waterHeightInches = newWaterHeightFeet.Value * 12.0;

                double groundHeightInches = 0;
                if (location.GroundHeight.HasValue)
                {
                    groundHeightInches = location.GroundHeight.Value;
                }
                double distanceReadingInches = groundHeightInches - waterHeightInches;

                // Not really sure whether we'll ever look at these logs, but it seems worthwhile to track these
                // separately...
                if (!reading.WaterHeight.HasValue)
                {
                    updStat.sbDetails.AppendFormat("Device {0}: Received height reading of {1} at {2}\n", device.DeviceId, newWaterHeightFeet.Value, reading.Timestamp);
                }
                else if (reading.WaterHeight.Value != waterHeightInches)
                {
                    updStat.sbDetails.AppendFormat("Device {0}: Updating height reading from {1} to {2} at {3}\n",
                                                   device.DeviceId,
                                                   reading.WaterHeight.Value,
                                                   newWaterHeightFeet.Value,
                                                   reading.Timestamp);
                }

                if (!reading.WaterHeight.HasValue || (reading.WaterHeight.Value != waterHeightInches))
                {
                    reading.GroundHeight = groundHeightInches;
                    reading.DistanceReading = distanceReadingInches;
                    reading.WaterHeight = waterHeightInches;

                    reading.AdjustReadingForLocation(location);
                    reading.AdjustReadingForDevice(device);
                    updStat.heightReadingCount++;
                    dirty = true;
                }
            }
            return dirty;
        }

        private static bool UpdateReadingDischarge(SensorReading reading,
                                                   DeviceBase device,
                                                   SensorLocationBase location,
                                                   double? newDischarge,
                                                   UpdateStatus updStat)
        {
            bool dirty = false;
            if (!newDischarge.HasValue)
            {
                if (reading.WaterDischarge.HasValue)
                {
                    // Clear out the discharge; apparently a previously-valid-looking data point was discarded by USGS.
                    // NOTE: We are using a sentinel here to indicate invalid data because we need to be able to distinguish
                    // between "We haven't received data yet" and "We have received an indication that USGS has no data".
                    reading.WaterDischarge = INVALID_READING_SENTINEL;
                    updStat.sbDetails.AppendFormat("Device {0}: Removing discharge reading at {1}\n", device.DeviceId, reading.Timestamp);
                    dirty = true;
                    updStat.dischargeReadingCount++;
                }
            }
            else
            {
                // Not really sure whether we'll ever look at these logs, but it seems worthwhile to track these
                // separately...
                if (!reading.WaterDischarge.HasValue)
                {
                    updStat.sbDetails.AppendFormat("Device {0}: Received discharge reading of {1} at {2}\n", device.DeviceId, newDischarge.Value, reading.Timestamp);
                }
                else if (reading.WaterDischarge.Value != newDischarge.Value)
                {
                    updStat.sbDetails.AppendFormat("Device {0}: Updating discharge reading from {1} to {2} at {3}\n",
                                                   device.DeviceId,
                                                   reading.WaterDischarge.Value,
                                                   newDischarge.Value,
                                                   reading.Timestamp);
                }

                if (!reading.WaterDischarge.HasValue || (reading.WaterDischarge.Value != newDischarge.Value))
                {
                    reading.WaterDischarge = newDischarge;
                    dirty = true;
                    updStat.dischargeReadingCount++;
                }
            }
            return dirty;
        }

        private static int EnsureSensorReadingForTimestamp(List<SensorReading> loadedReadings,
                                                           List<bool> dirty,
                                                           DateTime timestamp,
                                                           string listenerInfo,
                                                           int locationId,
                                                           DeviceBase device)
        {
            for (int i = 0; i < loadedReadings.Count; i++)
            {
                if (loadedReadings[i].Timestamp == timestamp)
                {
                    return i;
                }
            }
            SensorReading reading = new SensorReading()
            {
                ListenerInfo = listenerInfo,
                Timestamp = timestamp,
                LocationId = locationId,
                DeviceId = device.DeviceId,
            };
            if (loadedReadings.Count != dirty.Count)
            {
                throw new ApplicationException(String.Format("Save-tracking count mismatch in {0}: {1} != {2}", listenerInfo, loadedReadings.Count, dirty.Count));
            }
            loadedReadings.Add(reading);
            dirty.Add(true);
            return loadedReadings.Count - 1;
        }
    }
}
