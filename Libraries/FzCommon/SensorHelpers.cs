using System.Text;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace FzCommon
{
    public class SenixReadingResult
    {
        public bool ShouldSave;
        public bool ShouldSaveAsDeleted;
        public string Result;
    }

    // NOTE: This does not have all the same responsibilities as the other two SensorHelper classes; it is
    // not interchangeable with them.
    public class DraginoSensorHelper
    {
        public static bool ShouldIgnoreReading(
            dynamic postData,
            SenixReadingResult result,
            out string deleteReason
        )
        {
            // If this reading appears to come from a Dragino sensor, validate that the signal strength
            // is good.  If it doesn't appear to come from a Dragino sensor, return false and leave reason empty.
            deleteReason = null;
            if (!postData.ContainsKey("uplink_message"))
            {
                return false;
            }
            var uplink = postData["uplink_message"];
            if (uplink["decoded_payload"] == null)
            {
                return false;
            }
            var decoded_payload = uplink["decoded_payload"];
            if (decoded_payload["Distance_signal_strength"] == null)
            {
                return false;
            }
            int strength = (int)(decoded_payload["Distance_signal_strength"]);
            if (strength < 100)
            {
                // If we have a potential 'result' object, set it to save as deleted
                if (result != null)
                {
                    deleteReason = "Filtered (Distance signal strength too low)";
                    result.Result = "Filtered (Distance signal strength too low)";
                    result.ShouldSave = true;
                    result.ShouldSaveAsDeleted = true;
                    return false;
                }
                return true;
            }
            if (strength >= 65535)
            {
                // If we have a potential 'result' object, set it to save as deleted
                if (result != null)
                {
                    deleteReason = "Filtered (Distance signal strength above 65534)";
                    result.Result = "Filtered (Distance signal strength above 65534)";
                    result.ShouldSave = true;
                    result.ShouldSaveAsDeleted = true;
                    return false;
                }
                return true;
            }
            return false;
        }
    }

    public class SenixSensorHelper
    {
        public static int DefaultSampleRate = 15; // minutes

        public static SenixReadingResult ProcessReading(
            SqlConnection sqlcn,
            SensorReading reading,
            dynamic senixData,
            out string externalDeviceId,
            out DeviceBase device
        )
        {
            externalDeviceId = null;
            device = null;
            SenixReadingResult result = new SenixReadingResult()
            {
                ShouldSave = false,
                ShouldSaveAsDeleted = false,
            };

            string externalId = senixData["eui"];
            externalDeviceId = externalId;
            device = DeviceBase.GetDeviceByExternalId(sqlcn, externalId);
            if (device == null)
            {
                //$ TODO: how to handle this?
                result.Result = String.Format("Ignored (can't find eui {0})", externalId);
                return result;
            }

            string deleteReason = null;
            if (ShouldIgnoreReading(senixData, result, out deleteReason) && !result.ShouldSave)
            {
                return result;
            }

            if (!device.LocationId.HasValue || device.LocationId.Value == 0)
            {
                //$ TODO: how to handle this?
                result.Result = String.Format("Ignored (Device {0} has no location)", externalId);
                return result;
            }

            SensorLocationBase location = SensorLocationBase.GetLocation(
                sqlcn,
                device.LocationId.Value
            );
            if (location != null)
            {
                location.ConvertValuesForDisplay();
            }

            double distanceReading = (double)senixData["Distance"];
            double waterHeight = (double)senixData["LOffset"] - distanceReading;
            double groundHeight = location == null ? 0 : (location.GroundHeight ?? 0);

            // The "time" field can come in either as a nicely formatted string like "2024-01-27T03:44:51.82158Z"
            // or as a number like 1706327019745 which should be interpreted as a UTC Milliseconds time.
            string timeAsString = (string)senixData["time"];
            long utcMsec;
            bool parsedAsLong = false;
            DateTime timeStamp = DateTime.UtcNow;
            if (long.TryParse(timeAsString, out utcMsec))
            {
                DateTime parsedTime = DateTimeOffset.FromUnixTimeMilliseconds(utcMsec).UtcDateTime;
                // Sanity check.  This is a little silly, but we might as well verify that we're in
                // the ballpark.
                if (parsedTime.Year > 2020 && parsedTime.Year < 3000)
                {
                    timeStamp = parsedTime;
                    parsedAsLong = true;
                }
            }
            if (!parsedAsLong)
            {
                try
                {
                    timeStamp = DateTime.Parse(timeAsString);
                }
                catch
                {
                    ErrorManager.ReportError(
                        ErrorSeverity.Major,
                        "SenixListener",
                        "Error: could not parse timestamp string '" + timeAsString + "'"
                    );
                    timeStamp = DateTime.UtcNow;
                }
            }

            // These are currently in feet; convert to inches for storage.  Also distanceReading is negative...
            distanceReading *= -12.0;
            waterHeight *= 12.0;
            groundHeight *= 12.0;

            reading.DeleteReason = deleteReason;
            reading.IsFiltered = (result.ShouldSaveAsDeleted);
            reading.Timestamp = timeStamp;
            reading.DeviceTimestamp = timeStamp;
            reading.LocationId = device.LocationId.Value;
            reading.DeviceId = device.DeviceId;
            reading.GroundHeight = groundHeight;
            reading.DistanceReading = distanceReading;
            reading.WaterHeight = waterHeight;
            reading.BatteryVolt = (int)((double)senixData["TransmitterBat"] * 1000.0);
            reading.RawSensorData = senixData;

            reading.RSSI = (double)senixData["RSSI"];
            reading.SNR = (double)senixData["SNR"];

            reading.AdjustReadingForLocation(location);
            reading.AdjustReadingForDevice(device);

            if (location.IsOffline)
            {
                reading.DeleteReason = "Filtered (Location offline)";
                result.Result = "Filtered (Location offline)";
                reading.IsFiltered = true;
                reading.IsDeleted = true;
                result.ShouldSaveAsDeleted = true;
            }
            else
            {
                // Look for a recent reading to compare with.
                SensorReading lastReading = SensorReading
                    .GetLatestReadingForLocation(location.Id)
                    .Result;
                if (lastReading != null)
                {
                    if (
                        lastReading.Timestamp.AddMinutes(
                            FzCommon.Constants.ChangeThresholdMaxMinutes
                        ) >= reading.Timestamp
                    )
                    {
                        double delta =
                            Math.Abs(
                                reading.WaterHeightFeet.Value - lastReading.WaterHeightFeet.Value
                            ) / ((reading.Timestamp - lastReading.Timestamp).TotalHours);
                        double threshold =
                            location.MaxChangeThreshold
                            ?? FzCommon.Constants.DefaultMaxValidChangeThreshold;
                        if (delta > threshold)
                        {
                            reading.DeleteReason = "Filtered (change too big)";
                            reading.IsFiltered = true;
                            reading.IsDeleted = true;
                            result.ShouldSaveAsDeleted = true;
                            result.Result = String.Format(
                                "filtered (delta {0:0.00} feet/hour > threshold {1:0.00})",
                                delta,
                                threshold
                            );

                            Task.WaitAll(
                                SlackClient.SendFilteredReadingNotification(
                                    sqlcn,
                                    location,
                                    reading,
                                    delta,
                                    threshold
                                )
                            );
                        }
                    }
                }
            }

            if (senixData["discard"] != "true")
            {
                result.ShouldSave = true;
            }
            else
            {
                result.Result = "Discarded per flag";
            }

            return result;
        }

        public static dynamic GetRawData(SensorReading sr)
        {
            string rawData = (string)sr.RawSensorData;

            // Early sensor readings were serialized wrong...
            if (((string)rawData)[0] == '"')
            {
                rawData = (string)JsonConvert.DeserializeObject(rawData);
            }
            return JsonConvert.DeserializeObject(rawData);
        }

        // note: result can be null; not everybody needs it...
        public static bool ShouldIgnoreReading(
            dynamic senixData,
            SenixReadingResult result,
            out string deleteReason
        )
        {
            deleteReason = null;

            // Figure out if this is the "old style" receiver format or the "new style" receiver format.
            if (senixData.ContainsKey("uplink_message"))
            {
                // For now, our only new-style receiver messages that we may want to discard are
                // Dragino sensor readings.
                if (DraginoSensorHelper.ShouldIgnoreReading(senixData, result, out deleteReason))
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (senixData.ContainsKey("cmd"))
                {
                    int cmd = (int)senixData["cmd"];
                    if (cmd != 64)
                    {
                        // Only cmd 64 corresponds to an actual measurement...
                        if (result != null)
                        {
                            result.Result = "Ignored (cmd != 64)";
                        }
                        return true;
                    }
                }

                int ecode = (int)senixData["ecode"];
                string alarm = (string)senixData["alarm"];
                if (ecode == 3 && alarm == "fault")
                {
                    // This combination seems to indicate a specific "Sensor Target Loss" failure from the sensor...
                    if (result != null)
                    {
                        deleteReason = "Filtered (Sensor Target Loss)";
                        result.Result = "Saved as deleted (Sensor Target Loss)";

                        // For now: record this reading as deleted if we can (i.e. if we have a result
                        // object in which to convey that desire).
                        result.ShouldSave = true;
                        result.ShouldSaveAsDeleted = true;
                        return false;
                    }
                    return true;
                }

                if (!senixData.ContainsKey("LScale"))
                {
                    deleteReason = "Filtered (LScale is missing)";
                    if (result != null)
                    {
                        result.Result = "Saved as deleted (LScale is missing)";
                        result.ShouldSave = true;
                        result.ShouldSaveAsDeleted = true;
                    }
                    return false;
                }
            }

            return false;
        }

        public static int GetSampleRate(dynamic senixData)
        {
            try
            {
                if (
                    !String.IsNullOrEmpty((string)senixData["SampleRate1"])
                    && !String.IsNullOrEmpty((string)senixData["SampleRate2"])
                )
                {
                    int sampleRate1 = (int)senixData["SampleRate1"];
                    int sampleRate2 = (int)senixData["SampleRate2"];

                    if (sampleRate1 != sampleRate2)
                    {
                        //$ TODO: Notify admins about this somehow.
                    }
                    return Math.Max(sampleRate1 / 60, 1);
                }
            }
            catch (Exception ex)
            {
                //$ TODO: Do we want to do more than log this somewhere?  It represents unexpected data coming
                //$ from the receiver, but it's not business-critical...
                ErrorManager.ReportException(
                    ErrorSeverity.Minor,
                    "SenixSensorHelper.GetSampleRate",
                    ex
                );
            }
            return DefaultSampleRate;
        }

        public static int GetSampleRate(SensorReading sr)
        {
            dynamic senixData = GetRawData(sr);
            return GetSampleRate(senixData);
        }

        public static SenixReadingResult ReplayReading(
            SqlConnection sqlcn,
            int readingId,
            out SensorReading originalReading,
            out SensorReading newReading
        )
        {
            // Load two copies, just to be sure our original is untouched.
            originalReading = LoadReading(sqlcn, readingId);
            newReading = LoadReading(sqlcn, readingId);

            DeviceBase device;
            string externalDeviceId = null;
            dynamic senixData = JsonConvert.DeserializeObject(
                (string)originalReading.RawSensorData
            );
            return SenixSensorHelper.ProcessReading(
                sqlcn,
                newReading,
                senixData,
                out externalDeviceId,
                out device
            );
        }

        private static SensorReading LoadReading(SqlConnection sqlcn, int readingId)
        {
            Task<SensorReading> task = SensorReading.GetReading(sqlcn, readingId);
            task.Wait();
            return task.Result;
        }
    }

    public class ThingsNetworkHelper
    {
        public static SenixReadingResult ProcessReading(
            SqlConnection sqlcn,
            SensorReading reading,
            dynamic postData,
            out string externalDeviceId,
            out string receiverId,
            out DeviceBase device
        )
        {
            externalDeviceId = null;
            receiverId = null;
            device = null;
            SenixReadingResult result = new SenixReadingResult()
            {
                ShouldSave = false,
                ShouldSaveAsDeleted = false,
            };
            string externalId = NormalizeEui((string)(postData["end_device_ids"]["dev_eui"]));
            externalDeviceId = externalId;
            device = DeviceBase.GetDeviceByExternalId(sqlcn, externalId);
            if (device == null)
            {
                //$ TODO: how to handle this?
                result.Result = String.Format("Ignored (can't find eui {0})", externalId);
                return result;
            }
            var uplink = postData["uplink_message"];
            var rx_metadata = uplink["rx_metadata"][0];
            receiverId = NormalizeEui((string)(rx_metadata["gateway_ids"]["eui"]));

            string deleteReason = null;
            if (ShouldIgnoreReading(postData, result, out deleteReason) && !result.ShouldSave)
            {
                return result;
            }

            if (!device.LocationId.HasValue || device.LocationId.Value == 0)
            {
                //$ TODO: how to handle this?
                result.Result = String.Format("Ignored (Device {0} has no location)", externalId);
                return result;
            }

            SensorLocationBase location = SensorLocationBase.GetLocation(
                sqlcn,
                device.LocationId.Value
            );
            if (location != null)
            {
                location.ConvertValuesForDisplay();
            }
            if (uplink["decoded_payload"] == null)
            {
                result.Result = "Ignored (no decoded_payload found in reading data)";
                return result;
            }
            var decoded_payload = uplink["decoded_payload"];

            double distanceFeet;
            if (decoded_payload["distance_feet"] != null)
            {
                distanceFeet = (double)(decoded_payload["distance_feet"]);
            }
            else if (decoded_payload["Distance_cm"] != null)
            {
                double distanceCm = (double)(decoded_payload["Distance_cm"]);
                distanceFeet = distanceCm * 0.0328084;
            }
            else if (decoded_payload["distance"] != null)
            {
                double distanceMeters = (double)(decoded_payload["distance"]);
                distanceFeet = distanceMeters * 3.28084;
            }
            else
            {
                result.Result = "Ignored (decoded_payload doesn't have a recognizable distance value)";
                return result;
            }
            double groundHeight = location == null ? 0 : (location.GroundHeight ?? 0);

            string timeAsString = null;
            try
            {
                timeAsString = (string)(uplink["settings"]["time"]);
            }
            catch
            {
                timeAsString = null;
            }
            if (String.IsNullOrEmpty(timeAsString))
            {
                timeAsString = (string)(uplink["received_at"]);
            }
            DateTime timeStamp = DateTime.UtcNow;
            try
            {
                timeStamp = DateTime.Parse(timeAsString);
            }
            catch
            {
                ErrorManager.ReportError(
                    ErrorSeverity.Major,
                    "SenixListener",
                    "Warning: could not parse timestamp string '" + timeAsString + "'; saving with timestamp 'now'"
                );
                timeStamp = DateTime.UtcNow;
            }

            // These are currently in feet; convert to inches for storage.
            double distanceReading = distanceFeet * 12.0;
            groundHeight *= 12.0;

            reading.DeleteReason = deleteReason;
            reading.IsFiltered = (result.ShouldSaveAsDeleted);
            reading.Timestamp = timeStamp;
            reading.DeviceTimestamp = timeStamp;
            reading.LocationId = device.LocationId.Value;
            reading.DeviceId = device.DeviceId;
            reading.GroundHeight = groundHeight;
            reading.DistanceReading = distanceReading;
            reading.WaterHeight = distanceReading;
            reading.BatteryPercent = 0;
            if (uplink["last_battery_percentage"] != null && uplink["last_battery_percentage"]["value"] != null)
            {
                reading.BatteryPercent = (double)(uplink["last_battery_percentage"]["value"]);
            }
            else if (decoded_payload["battery_percentage"] != null)
            {
                reading.BatteryPercent = (double)(decoded_payload["battery_percentage"]);
            }

            // NOTE: This is dumb.  reading.BatteryVolt is actually in millivolts.  I apologize.
            reading.BatteryVolt = 0;
            if (decoded_payload["battery_voltage_mv"] != null)
            {
                reading.BatteryVolt = (int)(decoded_payload["battery_voltage_mv"]);
            }
            else if (decoded_payload["battery_voltage_v"] != null)
            {
                reading.BatteryVolt = (int)(1000.0 * (double)(decoded_payload["battery_voltage_v"]));
            }
            else if (decoded_payload["batV"] != null)
            {
                reading.BatteryVolt = (int)(1000.0 * (double)(decoded_payload["batV"]));
            }

            reading.RawSensorData = postData;

            reading.RSSI = (double)rx_metadata["rssi"];
            reading.SNR = (double)rx_metadata["snr"];

            reading.AdjustReadingForLocation(location);
            reading.AdjustReadingForDevice(device);

            if (location.IsOffline)
            {
                reading.DeleteReason = "Filtered (Location offline)";
                result.Result = "Filtered (Location offline)";
                reading.IsFiltered = true;
                reading.IsDeleted = true;
                result.ShouldSaveAsDeleted = true;
            }
            else
            {
                // Look for a recent reading to compare with.
                SensorReading lastReading = SensorReading
                    .GetLatestReadingForLocation(location.Id)
                    .Result;
                if (lastReading != null)
                {
                    if (
                        lastReading.Timestamp.AddMinutes(
                            FzCommon.Constants.ChangeThresholdMaxMinutes
                        ) >= reading.Timestamp
                    )
                    {
                        double delta =
                            Math.Abs(
                                reading.WaterHeightFeet.Value - lastReading.WaterHeightFeet.Value
                            ) / ((reading.Timestamp - lastReading.Timestamp).TotalHours);
                        double threshold =
                            location.MaxChangeThreshold
                            ?? FzCommon.Constants.DefaultMaxValidChangeThreshold;
                        if (delta > threshold)
                        {
                            reading.DeleteReason = "Filtered (change too big)";
                            reading.IsFiltered = true;
                            reading.IsDeleted = true;
                            result.ShouldSaveAsDeleted = true;
                            result.Result = String.Format(
                                "filtered (delta {0:0.00} feet/hour > threshold {1:0.00})",
                                delta,
                                threshold
                            );

                            Task.WaitAll(
                                SlackClient.SendFilteredReadingNotification(
                                    sqlcn,
                                    location,
                                    reading,
                                    delta,
                                    threshold
                                )
                            );
                        }
                    }
                }
            }

            if (postData["discard"] != "true")
            {
                result.ShouldSave = true;
            }
            else
            {
                result.Result = "Discarded per flag";
            }

            return result;
        }

        // The rest of the Floodzilla stuff expects EUIs to be in the format 11-22-33-44-55-66-aa-bb
        public static string NormalizeEui(string eui)
        {
            char[] nibbles = eui.ToLower().ToCharArray();
            if (nibbles.Length != 16)
            {
                ErrorManager.ReportError(ErrorSeverity.Major, "ThingsNetworkHelper", String.Format("Unexpected format for EUI '{0}'", eui));
                return null;
            }
            StringBuilder sbRet = new();
            int pos = 0;
            for (int i = 0; i < 8; i++)
            {
                if (pos > 0)
                {
                    sbRet.Append('-');
                }
                sbRet.Append(nibbles[pos++]);
                sbRet.Append(nibbles[pos++]);
            }
            return sbRet.ToString();
        }

        // note: result can be null; not everybody needs it...
        public static bool ShouldIgnoreReading(
            dynamic postData,
            SenixReadingResult result,
            out string deleteReason
        )
        {
            deleteReason = null;

            // If this is a Dragino sensor, and we should ignore the reading, do so.
            if (DraginoSensorHelper.ShouldIgnoreReading(postData, result, out deleteReason))
            {
                return true;
            }
            return false;
        }

        public static SenixReadingResult ReplayReading(
            SqlConnection sqlcn,
            int readingId,
            out SensorReading originalReading,
            out SensorReading newReading
        )
        {
            // Load two copies, just to be sure our original is untouched.
            originalReading = LoadReading(sqlcn, readingId);
            newReading = LoadReading(sqlcn, readingId);

            DeviceBase device;
            string externalDeviceId = null;
            string receiverId = null;
            dynamic senixData = JsonConvert.DeserializeObject(
                (string)originalReading.RawSensorData
            );
            return ThingsNetworkHelper.ProcessReading(
                sqlcn,
                newReading,
                senixData,
                out externalDeviceId,
                out receiverId,
                out device
            );
        }

        private static SensorReading LoadReading(SqlConnection sqlcn, int readingId)
        {
            Task<SensorReading> task = SensorReading.GetReading(sqlcn, readingId);
            task.Wait();
            return task.Result;
        }
    }
}
