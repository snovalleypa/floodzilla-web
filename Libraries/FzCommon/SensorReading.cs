using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    // This is handy but it really slows down the debugger...
    [DebuggerDisplay("{WaterHeightFeet}ft @ {Timestamp}")]
    public class SensorReading
    {
        public int Id { get; set; }
        
        // This is used to tag readings with metadata about the listener version, etc, for debugging purposes.
        [JsonProperty(PropertyName = "ListenerInfo")]
        public string ListenerInfo { get; set; }

        // If a reading has been deleted/undeleted, this is the last reason why
        [JsonProperty(PropertyName = "DeleteReason")]
        public string DeleteReason { get; set; }

        [JsonProperty(PropertyName = "Timestamp")]
        public DateTime Timestamp { get; set; }

        // If provided by the device
        [JsonProperty(PropertyName = "DeviceTimestamp")]
        public DateTime DeviceTimestamp { get; set; }

        [JsonProperty(PropertyName = "LocationId")]
        public int? LocationId { get; set; }

        [JsonProperty(PropertyName = "DeviceId")]
        public int? DeviceId { get; set; }

        [JsonProperty(PropertyName = "DeviceTypeId")]
        public int? DeviceTypeId { get; set; }

        //$ TODO: Convert all values from inches to feet

        // This is pulled from the location.  Inches above sea level.
        [JsonProperty(PropertyName = "GroundHeight")]
        public double? GroundHeight { get; set; }

        // The distance value returned by the sensor.  Inches.  Should be > 0.
        [JsonProperty(PropertyName = "DistanceReading")]
        public double? DistanceReading { get; set; }

        // Calculated water height in inches; if the location has BenchmarkElevation and RelativeSensorHeight, this
        // will be height above sea level.
        [JsonProperty(PropertyName = "RawWaterHeight")]
        public double? RawWaterHeight { get; set; }

        // "Final" water height in inches, clipped to GroundHeight if necessary; if the location has
        // BenchmarkElevation and RelativeSensorHeight, this will be height above sea level.
        [JsonProperty(PropertyName = "WaterHeight")]
        public double? WaterHeight { get; set; }

        // These are temporary; they are the same as the previous four entries, but in feet.
        // Eventually we'll convert to only using feet for storage.
        public double? GroundHeightFeet { get; set; }
        public double? DistanceReadingFeet { get; set; }
        public double? RawWaterHeightFeet { get; set; }
        public double? WaterHeightFeet { get; set; }

        // Water flow rate in CFS.  Only provided for USGS gages.
        [JsonProperty(PropertyName = "WaterDischarge")]
        public double? WaterDischarge { get; set; }

        // Calculated Water flow rate in CFS based on gage height.  Only provided for USGS gages.
        public double? CalcWaterDischarge { get; set; }

        // Current battery level in millivolts.
        [JsonProperty(PropertyName = "BatteryVolt")]
        public int? BatteryVolt { get; set; }

        // RSSI and signal-to-noise ratio (for radio-using sensors)
        public double? RSSI { get; set; }
        public double? SNR { get; set; }

        // The actual type and data depends on the sensor type.
        [JsonProperty(PropertyName = "RawSensorData" )]
        public object RawSensorData { get; set; }

        public bool IsDeleted { get; set; }

        // True if this reading was discarded for being out of range; IsDeleted should also be true.
        public bool IsFiltered { get; set; }

        // Snapshotted from the location.  Feet above sea level.
        public double? BenchmarkElevation { get; set; }

        // These are snapshotted from the location; all are in feet relative to benchmark
        public double? RelativeSensorHeight { get; set; }
        public double? Green { get; set; }
        public double? Brown { get; set; }
        public double? RoadSaddleHeight { get; set; }
        public double? MarkerOneHeight { get; set; }
        public double? MarkerTwoHeight { get; set; }

        // Like above but converted to ASL
        public double? GreenASL { get { return this.ConvertToASL(this.Green); }}
        public double? BrownASL { get { return this.ConvertToASL(this.Brown); }}

        public SensorReading()
        {
        }

        public SensorReading(SensorReading source)
        {
            this.ListenerInfo = source.ListenerInfo;
            this.DeleteReason = source.DeleteReason;
            this.Timestamp = source.Timestamp;
            this.DeviceTimestamp = source.DeviceTimestamp;
            this.LocationId = source.LocationId;
            this.DeviceId = source.DeviceId;
            this.DeviceTypeId = source.DeviceTypeId;
            this.GroundHeight = source.GroundHeight;
            this.DistanceReading = source.DistanceReading;
            this.RawWaterHeight = source.RawWaterHeight;
            this.WaterHeight = source.WaterHeight;
            this.GroundHeightFeet = source.GroundHeightFeet;
            this.DistanceReadingFeet = source.DistanceReadingFeet;
            this.RawWaterHeightFeet = source.RawWaterHeightFeet;
            this.WaterHeightFeet = source.WaterHeightFeet;
            this.WaterDischarge = source.WaterDischarge;
            this.CalcWaterDischarge = source.CalcWaterDischarge;
            this.BatteryVolt = source.BatteryVolt;
            this.RawSensorData = source.RawSensorData;
            this.IsDeleted = source.IsDeleted;
            this.IsFiltered = source.IsFiltered;
            this.BenchmarkElevation = source.BenchmarkElevation;
            this.RelativeSensorHeight = source.RelativeSensorHeight;
            this.Green = source.Green;
            this.Brown = source.Brown;
            this.RoadSaddleHeight = source.RoadSaddleHeight;
            this.MarkerOneHeight = source.MarkerOneHeight;
            this.MarkerTwoHeight = source.MarkerTwoHeight;
            this.RSSI = source.RSSI;
            this.SNR = source.SNR;
        }

        // Sets WaterHeight, taking into account benchmark level and offset from the location. Also
        // snapshots some data from the location into the reading record.
        public void AdjustReadingForLocation(SqlConnection sqlConnection, int locationId)
        {
            SensorLocationBase location = SensorLocationBase.GetLocation(sqlConnection, locationId);
            if (location != null)
            {
                location.ConvertValuesForDisplay();
            }
            this.AdjustReadingForLocation(location);
        }

        // This assumes ConvertValuesForDisplay() has been called on the location.
        public void AdjustReadingForLocation(SensorLocationBase location)
        {
            if (location == null)
            {
                return;
            }

            if ((location.BenchmarkElevation ?? 0) != 0 && (location.RelativeSensorHeight ?? 0) != 0)
            {
                double sensorHeight = (location.RelativeSensorHeight.Value * 12.0);
                this.WaterHeight = sensorHeight - this.DistanceReading;
                this.RawWaterHeight = this.WaterHeight;
                if (this.GroundHeight != 0 && this.WaterHeight < this.GroundHeight)
                {
                    this.WaterHeight = this.GroundHeight;
                }
            }
            else
            {
                this.RawWaterHeight = this.WaterHeight;
            }

            if (location.BenchmarkElevation.HasValue)
            {
                this.BenchmarkElevation = location.BenchmarkElevation;

                // Record all of these relative to benchmark.
                this.RelativeSensorHeight = location.RelativeSensorHeight - this.BenchmarkElevation;
                this.Green = location.Green - this.BenchmarkElevation;
                this.Brown = location.Brown - this.BenchmarkElevation;
                this.RoadSaddleHeight = location.RoadSaddleHeight - this.BenchmarkElevation;
                this.MarkerOneHeight = location.MarkerOneHeight - this.BenchmarkElevation;
                this.MarkerTwoHeight = location.MarkerTwoHeight - this.BenchmarkElevation;
            }

            // These are temporary -- eventually everything will just be in feet
            this.GroundHeightFeet = FzCommonUtility.GetRoundValue(this.GroundHeight / 12.0);
            this.DistanceReadingFeet = FzCommonUtility.GetRoundValue(this.DistanceReading / 12.0);
            this.RawWaterHeightFeet = FzCommonUtility.GetRoundValue(this.RawWaterHeight / 12.0);
            this.WaterHeightFeet = FzCommonUtility.GetRoundValue(this.WaterHeight / 12.0);
        }

        public void AdjustReadingForDevice(SqlConnection sqlConnection, int deviceId)
        {
            DeviceBase device = DeviceBase.GetDevice(sqlConnection, deviceId);
            this.AdjustReadingForDevice(device);
        }

        public void AdjustReadingForDevice(DeviceBase device)
        {
            if (device == null)
            {
                return;
            }
            this.DeviceTypeId = device.DeviceTypeId;

            //$ TODO: Filter out this reading if it's outside the min/max range for this device
        }

        private double? ConvertToASL(double? raw)
        {
            if (raw == null || this.BenchmarkElevation == null)
            {
                return null;
            }
            return raw + this.BenchmarkElevation;
        }

        public async Task Save(SqlConnection sqlConnection)
        {
            await this.SaveSQL(sqlConnection);
        }

        private async Task SaveSQL(SqlConnection sqlConnection)
        {
            SqlCommand cmd = new SqlCommand("SaveSensorReading", sqlConnection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@ListenerInfo", SqlDbType.VarChar, 200).Value = this.ListenerInfo;
            cmd.Parameters.Add("@Timestamp", SqlDbType.DateTime).Value = this.Timestamp;
            cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = this.LocationId;
            cmd.Parameters.Add("@DeviceId", SqlDbType.Int).Value = this.DeviceId;
            cmd.Parameters.Add("@DeviceTypeId", SqlDbType.Int).Value = this.DeviceTypeId;
            
            cmd.Parameters.Add("@GroundHeight", SqlDbType.Float).Value = this.GroundHeight;
            cmd.Parameters.Add("@DistanceReading", SqlDbType.Float).Value = this.DistanceReading;
            cmd.Parameters.Add("@RawWaterHeight", SqlDbType.Float).Value = this.RawWaterHeight;
            cmd.Parameters.Add("@WaterHeight", SqlDbType.Float).Value = this.WaterHeight;
            cmd.Parameters.Add("@WaterDischarge", SqlDbType.Float).Value = this.WaterDischarge;
            cmd.Parameters.Add("@CalcWaterDischarge", SqlDbType.Float).Value = this.CalcWaterDischarge;
            cmd.Parameters.Add("@BatteryVolt", SqlDbType.Int).Value = this.BatteryVolt;
            if (this.RawSensorData != null)
            {
                cmd.Parameters.Add("@RawSensorData", SqlDbType.Text).Value = JsonConvert.SerializeObject(this.RawSensorData);
            }
            else
            {
                cmd.Parameters.Add("@RawSensorData", SqlDbType.Text).Value = null;
            }

            cmd.Parameters.Add("@BenchmarkElevation", SqlDbType.Float).Value = this.BenchmarkElevation;
            cmd.Parameters.Add("@RelativeSensorHeight", SqlDbType.Float).Value = this.RelativeSensorHeight;
            cmd.Parameters.Add("@Green", SqlDbType.Float).Value = this.Green;
            cmd.Parameters.Add("@Brown", SqlDbType.Float).Value = this.Brown;
            cmd.Parameters.Add("@RoadSaddleHeight", SqlDbType.Float).Value = this.RoadSaddleHeight;
            cmd.Parameters.Add("@MarkerOneHeight", SqlDbType.Float).Value = this.MarkerOneHeight;
            cmd.Parameters.Add("@MarkerTwoHeight", SqlDbType.Float).Value = this.MarkerTwoHeight;

            cmd.Parameters.Add("@GroundHeightFeet", SqlDbType.Float).Value = this.GroundHeightFeet;
            cmd.Parameters.Add("@DistanceReadingFeet", SqlDbType.Float).Value = this.DistanceReadingFeet;
            cmd.Parameters.Add("@WaterHeightFeet", SqlDbType.Float).Value = this.WaterHeightFeet;
            cmd.Parameters.Add("@RawWaterHeightFeet", SqlDbType.Float).Value = this.RawWaterHeightFeet;

            cmd.Parameters.Add("@RSSI", SqlDbType.Float).Value = this.RSSI;
            cmd.Parameters.Add("@SNR", SqlDbType.Float).Value = this.SNR;
            
            cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = this.IsDeleted;
            cmd.Parameters.Add("@IsFiltered", SqlDbType.Bit).Value = this.IsFiltered;

            if (this.DeviceTimestamp != DateTime.MinValue)
            {
                cmd.Parameters.Add("@DeviceTimestamp", SqlDbType.DateTime).Value = this.DeviceTimestamp;
            }
            if (this.Id != 0)
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
            }
            if (this.DeleteReason != null)
            {
                cmd.Parameters.Add("@DeleteReason", SqlDbType.VarChar, 200).Value = this.DeleteReason;
            }
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (await dr.ReadAsync())
                {
                    this.Id = SqlHelper.Read<int>(dr, "Id");
                }
            }
        }

        protected static SensorReading InstantiateFromReader(SqlDataReader dr, Func<SensorReading> factory = null)
        {
            SensorReading sr;
            if (factory != null)
            {
                sr = factory();
            }
            else
            {
                sr = new SensorReading();
            }
            sr.Id                           = SqlHelper.Read<int>(dr, "Id");
            sr.ListenerInfo                 = SqlHelper.Read<string>(dr, "ListenerInfo");
            sr.DeleteReason                 = SqlHelper.Read<string>(dr, "DeleteReason");
            sr.Timestamp                    = SqlHelper.Read<DateTime>(dr, "Timestamp");
            sr.DeviceTimestamp              = SqlHelper.Read<DateTime>(dr, "DeviceTimestamp");
            sr.LocationId                   = SqlHelper.Read<int>(dr, "LocationId");
            sr.DeviceId                     = SqlHelper.Read<int>(dr, "DeviceId");
            sr.DeviceTypeId                 = SqlHelper.Read<int?>(dr, "DeviceTypeId");
            sr.GroundHeight                 = SqlHelper.Read<double?>(dr, "GroundHeight");
            sr.DistanceReading              = SqlHelper.Read<double?>(dr, "DistanceReading");
            sr.WaterHeight                  = SqlHelper.Read<double?>(dr, "WaterHeight");
            sr.RawWaterHeight               = SqlHelper.Read<double?>(dr, "RawWaterHeight");
            sr.GroundHeightFeet             = SqlHelper.Read<double?>(dr, "GroundHeightFeet");
            sr.DistanceReadingFeet          = SqlHelper.Read<double?>(dr, "DistanceReadingFeet");
            sr.WaterHeightFeet              = SqlHelper.Read<double?>(dr, "WaterHeightFeet");
            sr.RawWaterHeightFeet           = SqlHelper.Read<double?>(dr, "RawWaterHeightFeet");
            sr.WaterDischarge               = SqlHelper.Read<double?>(dr, "WaterDischarge");
            sr.CalcWaterDischarge           = SqlHelper.Read<double?>(dr, "CalcWaterDischarge");
            sr.BatteryVolt                  = SqlHelper.Read<int?>(dr, "BatteryVolt");
            sr.RawSensorData                = SqlHelper.Read<object>(dr, "RawSensorData");
            sr.IsDeleted                    = SqlHelper.Read<bool>(dr, "IsDeleted");
            sr.IsFiltered                   = SqlHelper.Read<bool>(dr, "IsFiltered");
            sr.BenchmarkElevation           = SqlHelper.Read<double?>(dr, "BenchmarkElevation");
            sr.RelativeSensorHeight         = SqlHelper.Read<double?>(dr, "RelativeSensorHeight");
            sr.Green                        = SqlHelper.Read<double?>(dr, "Green");
            sr.Brown                        = SqlHelper.Read<double?>(dr, "Brown");
            sr.RoadSaddleHeight             = SqlHelper.Read<double?>(dr, "RoadSaddleHeight");
            sr.MarkerOneHeight              = SqlHelper.Read<double?>(dr, "MarkerOneHeight");
            sr.MarkerTwoHeight              = SqlHelper.Read<double?>(dr, "MarkerTwoHeight");
            sr.RSSI                         = SqlHelper.Read<double?>(dr, "RSSI");
            sr.SNR                          = SqlHelper.Read<double?>(dr, "SNR");
            return sr;
        }

        protected static SensorReading InstantiateLimitedReadingFromReader(SqlDataReader dr, Func<SensorReading> factory = null)
        {
            SensorReading sr;
            if (factory != null)
            {
                sr = factory();
            }
            else
            {
                sr = new SensorReading();
            }
            sr.Id                           = SqlHelper.Read<int>(dr, "Id");
            sr.Timestamp                    = SqlHelper.Read<DateTime>(dr, "Timestamp");
            sr.LocationId                   = SqlHelper.Read<int>(dr, "LocationId");
            sr.WaterHeightFeet              = SqlHelper.Read<double?>(dr, "WaterHeightFeet");
            sr.WaterDischarge               = SqlHelper.Read<double?>(dr, "WaterDischarge");
            sr.BatteryVolt                  = SqlHelper.Read<int?>(dr, "BatteryVolt");
            sr.IsDeleted                    = SqlHelper.Read<bool>(dr, "IsDeleted");
            sr.RoadSaddleHeight             = SqlHelper.Read<double?>(dr, "RoadSaddleHeight");
            sr.GroundHeightFeet             = SqlHelper.Read<double?>(dr, "GroundHeightFeet");
            return sr;
        }

        protected static SensorReading InstantiateMinimalReadingFromReader(SqlDataReader dr, Func<SensorReading> factory = null)
        {
            SensorReading sr;
            if (factory != null)
            {
                sr = factory();
            }
            else
            {
                sr = new SensorReading();
            }
            sr.Id                           = SqlHelper.Read<int>(dr, "Id");
            sr.Timestamp                    = SqlHelper.Read<DateTime>(dr, "Timestamp");
            sr.LocationId                   = SqlHelper.Read<int>(dr, "LocationId");
            sr.WaterHeightFeet              = SqlHelper.Read<double?>(dr, "WaterHeightFeet");
            sr.WaterDischarge               = SqlHelper.Read<double?>(dr, "WaterDischarge");
            return sr;
        }

        public static async Task<SensorReading> GetReading(SqlConnection sqlcn, int readingId)
        {
            using (SqlCommand cmd = new SqlCommand("GetSensorReading", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = readingId;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (await dr.ReadAsync())
                    {
                        SensorReading sr = InstantiateFromReader(dr);
                        return sr;
                    }
                }
            }
            return null;
        }

        public static async Task<List<SensorReading>> GetReadingsForDevice(int deviceId,
                                                                           int? readingCount,
                                                                           DateTime? utcFromDate,
                                                                           DateTime? utcToDate,
                                                                           int skipCount = 0)
        {
            List<SensorReading> ret = new List<SensorReading>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                string procName = "GetSensorReadingsForDevice";
                if (skipCount == 0)
                {
                    procName = "GetSensorReadingsForDeviceNoSkip";
                }
                using (SqlCommand cmd = new SqlCommand(procName, sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@deviceId", SqlDbType.Int).Value = deviceId;
                    if (readingCount.HasValue) cmd.Parameters.Add("@readingCount", SqlDbType.Int).Value = readingCount;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                    if (skipCount > 0)
                    {
                        cmd.Parameters.Add("@skipCount", SqlDbType.Int).Value = skipCount;
                    }
                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            SensorReading sr = InstantiateFromReader(dr);
                            ret.Add(sr);
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<int> GetReadingCountForDevice(int deviceId,
                                                               DateTime? utcFromDate,
                                                               DateTime? utcToDate)
        {
            int count = 0;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                using (SqlCommand cmd = new SqlCommand("GetSensorReadingCountForDevice", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@deviceId", SqlDbType.Int).Value = deviceId;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;

                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (await dr.ReadAsync())
                        {
                            count = (int)dr["Count"];
                        }
                    }
                }
            }
            return count;
        }

        public static async Task<List<SensorReading>> GetReadingsForLocation(int locationId,
                                                                             int? readingCount,
                                                                             DateTime? utcFromDate,
                                                                             DateTime? utcToDate,
                                                                             int skipCount = 0,
                                                                             int lastReadingId = 0)
        {
            List<SensorReading> ret = new List<SensorReading>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                string procName = "GetSensorReadings";
                if (skipCount == 0)
                {
                    procName = "GetSensorReadingsNoSkip";
                }
                using (SqlCommand cmd = new SqlCommand(procName, sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                    if (readingCount.HasValue) cmd.Parameters.Add("@readingCount", SqlDbType.Int).Value = readingCount;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                    if (skipCount > 0)
                    {
                        cmd.Parameters.Add("@skipCount", SqlDbType.Int).Value = skipCount;
                    }
                    cmd.Parameters.Add("@lastReadingId", SqlDbType.Int).Value = lastReadingId;
                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            SensorReading sr = InstantiateFromReader(dr);
                            ret.Add(sr);
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<List<SensorReading>>
            GetMinimalReadingsForLocation(int locationId,
                                          int? readingCount,
                                          DateTime? utcFromDate,
                                          DateTime? utcToDate)
        {
            List<SensorReading> ret = new List<SensorReading>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                string procName = "GetMinimalSensorReadings";
                using (SqlCommand cmd = new SqlCommand(procName, sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                    if (readingCount.HasValue) cmd.Parameters.Add("@readingCount", SqlDbType.Int).Value = readingCount;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            SensorReading sr = InstantiateMinimalReadingFromReader(dr);
                            ret.Add(sr);
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<Dictionary<int, List<SensorReading>>>
            GetMinimalReadingsForLocations(SqlConnection sqlcn,
                                           List<int> locationIds,
                                           DateTime utcFromDate,
                                           DateTime utcToDate)
        {
            Dictionary<int, List<SensorReading>> ret = new();
            string idList = String.Join(',', locationIds);
            using (SqlCommand cmd = new SqlCommand("GetMinimalSensorReadingsForLocations", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@idList", SqlDbType.VarChar, 200).Value = idList;
                cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                List<SensorReading> curList = new();
                int curLocationId = -1;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        SensorReading sr = InstantiateMinimalReadingFromReader(dr);
                        if (!sr.LocationId.HasValue)
                        {
                            continue;
                        }
                        if (sr.LocationId.Value != curLocationId)
                        {
                            if (curLocationId != -1)
                            {
                                ret.Add(curLocationId, curList);
                                curList = new();
                            }
                            curLocationId = sr.LocationId.Value;
                        }
                        curList.Add(sr);
                    }
                }
                ret.Add(curLocationId, curList);
            }
            return ret;
        }

        public static async Task<List<SensorReading>>
            GetReadingsForLocationOldestFirst(int locationId,
                                              int? readingCount,
                                              DateTime? utcFromDate,
                                              DateTime? utcToDate,
                                              int lastReadingId = 0)
        {
            List<SensorReading> ret = new List<SensorReading>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                using (SqlCommand cmd = new SqlCommand("GetSensorReadingsOldestFirst", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                    if (readingCount.HasValue) cmd.Parameters.Add("@readingCount", SqlDbType.Int).Value = readingCount;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                    cmd.Parameters.Add("@lastReadingId", SqlDbType.Int).Value = lastReadingId;
                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            SensorReading sr = InstantiateFromReader(dr);
                            ret.Add(sr);
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<List<SensorReading>> GetAllReadingsForLocation(int locationId,
                                                                                int? readingCount,
                                                                                DateTime? utcFromDate,
                                                                                DateTime? utcToDate,
                                                                                int skipCount = 0,
                                                                                int lastReadingId = 0)
        {
            List<SensorReading> ret = new List<SensorReading>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                string procName = "GetAllSensorReadingsForLocation";
                if (skipCount == 0)
                {
                    procName = "GetAllSensorReadingsForLocationNoSkip";
                }
                using (SqlCommand cmd = new SqlCommand(procName, sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                    if (readingCount.HasValue) cmd.Parameters.Add("@readingCount", SqlDbType.Int).Value = readingCount;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                    if (skipCount > 0)
                    {
                        cmd.Parameters.Add("@skipCount", SqlDbType.Int).Value = skipCount;
                    }
                    cmd.Parameters.Add("@lastReadingId", SqlDbType.Int).Value = lastReadingId;
                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            SensorReading sr = InstantiateFromReader(dr);
                            ret.Add(sr);
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<List<SensorReading>> GetAllReadingsForDevice(int deviceId,
                                                                              int? readingCount,
                                                                              DateTime? utcFromDate,
                                                                              DateTime? utcToDate,
                                                                              int skipCount = 0,
                                                                              int lastReadingId = 0)
        {
            List<SensorReading> ret = new List<SensorReading>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                using (SqlCommand cmd = new SqlCommand("GetAllSensorReadingsForDevice", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@deviceId", SqlDbType.Int).Value = deviceId;
                    if (readingCount.HasValue) cmd.Parameters.Add("@readingCount", SqlDbType.Int).Value = readingCount;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;
                    cmd.Parameters.Add("@skipCount", SqlDbType.Int).Value = skipCount;
                    cmd.Parameters.Add("@lastReadingId", SqlDbType.Int).Value = lastReadingId;
                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            SensorReading sr = InstantiateFromReader(dr);
                            ret.Add(sr);
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<SensorReading> GetLatestReadingForLocation(int locationId)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                using (SqlCommand cmd = new SqlCommand("GetLatestSensorReadingForLocation", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;

                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (await dr.ReadAsync())
                        {
                            return InstantiateFromReader(dr);
                        }
                    }
                }
            }
            return null;
        }

        public static async Task<List<SensorReading>> GetLatestReadingsByLocation(SqlConnection sqlcn, int regionId)
        {
            List<SensorReading> ret = new List<SensorReading>();
            SqlCommand cmd = new SqlCommand("GetLatestSensorReadingsByLocation", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@RegionId", SqlDbType.Int).Value = regionId;
            
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (await dr.ReadAsync())
                {
                    ret.Add(InstantiateFromReader(dr));
                }
            }
            return ret;
        }

        public static async Task<int> GetReadingCountForLocation(int locationId,
                                                                 DateTime? utcFromDate,
                                                                 DateTime? utcToDate)
        {
            int count = 0;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                using (SqlCommand cmd = new SqlCommand("GetSensorReadingCount", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                    if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                    if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;

                    await sqlcn.OpenAsync();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (await dr.ReadAsync())
                        {
                            count = (int)dr["Count"];
                        }
                    }
                }
            }
            return count;
        }

        // NOTE: This doesn't return full objects; it only fills in the subset necessary for the front page UI...
        public static async Task<Dictionary<int, List<SensorReading>>> GetAllRecentReadings(SqlConnection sqlcn, DateTime utcFromDateTime, bool withDeleted)
        {
            Dictionary<int, List<SensorReading>> ret = new Dictionary<int, List<SensorReading>>();
            List<SensorReading> current = new List<SensorReading>();
            int currentLocationId = 0;
            string proc;
            if (withDeleted)
            {
                proc = "GetRecentSensorReadingsWithDeleted";
            }
            else
            {
                proc = "GetRecentSensorReadings";
            }
            using (SqlCommand cmd = new SqlCommand(proc, sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDateTime;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        SensorReading sr = InstantiateLimitedReadingFromReader(dr);
                        if (sr.LocationId.Value != currentLocationId)
                        {
                            if (current.Count > 0)
                            {
                                ret[currentLocationId] = current;
                                current = new List<SensorReading>();
                            }
                            currentLocationId = sr.LocationId.Value;
                        }
                        current.Add(sr);
                    }
                }
                if (current.Count > 0)
                {
                    ret[currentLocationId] = current;
                }
            }
            return ret;
        }

        // NOTE: This doesn't return full objects; it only fills in the subset necessary for the front page UI...
        public static async Task<Dictionary<int, List<SensorReading>>> GetAllSensorReadingsInTimespan(SqlConnection sqlcn, DateTime utcFromDateTime, DateTime utcToDateTime)
        {
            Dictionary<int, List<SensorReading>> ret = new Dictionary<int, List<SensorReading>>();
            List<SensorReading> current = new List<SensorReading>();
            int currentLocationId = 0;
            using (SqlCommand cmd = new SqlCommand("GetAllSensorReadingsInTimespan", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDateTime;
                cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDateTime;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        SensorReading sr = InstantiateLimitedReadingFromReader(dr);
                        if (sr.LocationId.Value != currentLocationId)
                        {
                            if (current.Count > 0)
                            {
                                ret[currentLocationId] = current;
                                current = new List<SensorReading>();
                            }
                            currentLocationId = sr.LocationId.Value;
                        }
                        current.Add(sr);
                    }
                }
                if (current.Count > 0)
                {
                    ret[currentLocationId] = current;
                }
            }
            return ret;
        }

        public static async Task<Dictionary<int, DateTime>> GetRecentSensorReadingTimestampsByLocationAsync(SqlConnection sqlcn, int regionId, DateTime utcFromDateTime, DateTime utcToDateTime)
        {
            Dictionary<int, DateTime> ret = new Dictionary<int, DateTime>();
            using (SqlCommand cmd = new SqlCommand("GetRecentSensorReadingTimestampsByLocation", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RegionId", SqlDbType.Int).Value = regionId;
                cmd.Parameters.Add("@fromDateTime", SqlDbType.DateTime).Value = utcFromDateTime;
                cmd.Parameters.Add("@toDateTime", SqlDbType.DateTime).Value = utcToDateTime;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        int locationId = SqlHelper.Read<int>(dr, "LocationId");
                        DateTime timestamp = SqlHelper.Read<DateTime>(dr, "timestamp");
                        ret[locationId] = timestamp;
                    }
                }
            }
            return ret;
        }

        public static async Task MarkReadingsAsDeleted(IEnumerable<int> readingIds, string deleteReason)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                await SqlHelper.CallIdListProcedureWithReason(sqlcn, "MarkSensorReadingsAsDeleted", readingIds, 180, deleteReason);
                sqlcn.Close();
            }
        }

        public static async Task MarkReadingsAsUndeleted(IEnumerable<int> readingIds, string undeleteReason)
        {

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                await SqlHelper.CallIdListProcedureWithReason(sqlcn, "MarkSensorReadingsAsUndeleted", readingIds, 180, undeleteReason);
                sqlcn.Close();
            }
        }
    }

    public class UsgsRawSensorData
    {
        [JsonProperty(PropertyName = "CalibrationId" )]
        public int? CalibrationId { get; set; }
    }

    //$ (daves) strongly type these
    public class SvpaRawSensorData
    {
        [JsonProperty(PropertyName = "Header")]
        public object Header;

        [JsonProperty(PropertyName = "SvpaSensorLevel1")]
        public object SvpaSensorLevel1;

        [JsonProperty(PropertyName = "SvpaSensorLevel2")]
        public object SvpaSensorLevel2;
    }
}
