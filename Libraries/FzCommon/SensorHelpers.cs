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
    
    public class SenixSensorHelper
    {
        public static int DefaultSampleRate = 15; // minutes

        public static SenixReadingResult ProcessReading(SqlConnection sqlcn, SensorReading reading, dynamic senixData, out string externalDeviceId, out DeviceBase device)
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

            SensorLocationBase location = SensorLocationBase.GetLocation(sqlcn, device.LocationId.Value);
            if (location != null)
            {
                location.ConvertValuesForDisplay();
            }

            double distanceReading = (double)senixData["Distance"];
            double waterHeight = (double)senixData["LOffset"] - distanceReading;
            double groundHeight = location == null ? 0 : (location.GroundHeight ?? 0);
            DateTime timeStamp = DateTime.Parse((string)senixData["time"]);

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
                SensorReading lastReading = SensorReading.GetLatestReadingForLocation(location.Id).Result;
                if (lastReading != null)
                {
                    if (lastReading.Timestamp.AddMinutes(FzCommon.Constants.ChangeThresholdMaxMinutes) >= reading.Timestamp)
                    {
                        double delta = Math.Abs(reading.WaterHeightFeet.Value - lastReading.WaterHeightFeet.Value) / ((reading.Timestamp - lastReading.Timestamp).TotalHours);
                        double threshold = location.MaxChangeThreshold ?? FzCommon.Constants.DefaultMaxValidChangeThreshold;
                        if (delta > threshold)
                        {
                            reading.DeleteReason = "Filtered (change too big)";
                            reading.IsFiltered = true;
                            reading.IsDeleted = true;
                            result.ShouldSaveAsDeleted = true;
                            result.Result = String.Format("filtered (delta {0:0.00} feet/hour > threshold {1:0.00})", delta, threshold);

                            Task.WaitAll(SlackClient.SendFilteredReadingNotification(sqlcn, location, reading, delta, threshold));
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
        public static bool ShouldIgnoreReading(dynamic senixData, SenixReadingResult result, out string deleteReason)
        {
            deleteReason = null;
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
                }
                result.ShouldSave = true;
                result.ShouldSaveAsDeleted = true;
                return false;
            }

            return false;
        }
        
        public static int GetSampleRate(dynamic senixData)
        {
            try
            {
                if (!String.IsNullOrEmpty((string)senixData["SampleRate1"]) && !String.IsNullOrEmpty((string)senixData["SampleRate2"]))
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
                ErrorManager.ReportException(ErrorSeverity.Minor, "SenixSensorHelper.GetSampleRate", ex);
            }
            return DefaultSampleRate;
        }

        public static int GetSampleRate(SensorReading sr)
        {
            dynamic senixData = GetRawData(sr);
            return GetSampleRate(senixData);
        }

        public static SenixReadingResult ReplayReading(SqlConnection sqlcn, int readingId, out SensorReading originalReading, out SensorReading newReading)
        {
            // Load two copies, just to be sure our original is untouched.
            originalReading = LoadReading(sqlcn, readingId);
            newReading = LoadReading(sqlcn, readingId);

            DeviceBase device;
            string externalDeviceId = null;
            dynamic senixData = JsonConvert.DeserializeObject((string)originalReading.RawSensorData);
            return SenixSensorHelper.ProcessReading(sqlcn, newReading, senixData, out externalDeviceId, out device);
        }

        private static SensorReading LoadReading(SqlConnection sqlcn, int readingId)
        {
            Task<SensorReading> task = SensorReading.GetReading(sqlcn, readingId);
            task.Wait();
            return task.Result;
        }
    }
}
