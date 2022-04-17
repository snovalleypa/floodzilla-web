using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class DeviceBase : ILogTaggable
    {
        internal class DeviceTaggableFactory : ILogBookTaggableFactory
        {
            public async Task<List<ILogTaggable>> GetAvailableTaggables(SqlConnection sqlcn, string category)
            {
                List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
                List<ILogTaggable> ret = new List<ILogTaggable>();
                foreach (DeviceBase device in devices)
                {
                    if (!device.IsDeleted)
                    {
                        ret.Add(device);
                    }
                }
                return ret;
            }
        }

        public const string TagCategory = "dev";

#region ILogTaggable
        public string GetTagCategory() { return DeviceBase.TagCategory; }
        public string GetTagId() { return this.DeviceId.ToString(); }
        public string GetTagName() { return "Device: " + this.Name; }
#endregion

        [Required]
        public int DeviceId { get; set; }

        [Required(ErrorMessage = "Device type is required.")]
        public int DeviceTypeId { get; set; }

        // For devices that identify themselves via a string Id (e.g. Senix devices)
        public string? ExternalDeviceId { get; set; }

        // For devices that represent USGS gages.
        public int? UsgsSiteId { get; set; }

        public string? Description { get; set; }
        public int? LocationId { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? Version { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? MaxStDev { get; set; }

        // For now, only USGS devices support discharge...
        public bool HasDischarge
        {
            get
            {
                return (this.DeviceTypeId == DeviceTypeIds.Usgs);
            }
        }

        // For all current sensors, this is read-only.  If future sensors support
        // changing the update interval, this should become read-write.
        public int? SensorUpdateInterval { get; private set; }

        public string? LatestReceiverId { get; set; }
        public DateTime? LastReadingReceived { get; set; }

        public static DeviceBase GetDeviceByExternalId(SqlConnection conn, string externalId)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Devices WHERE ExternalDeviceId = '{externalId}'", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "DeviceBase.GetDeviceByExternalId", ex);
            }
            return null;
        }

        public static DeviceBase GetDevice(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Devices WHERE DeviceId = '{id}'", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "DeviceBase.GetDevice", ex);
            }
            return null;
        }

        public static List<DeviceBase> GetDevices(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Devices", conn);
            try
            {
                List<DeviceBase> ret = new List<DeviceBase>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "DeviceBase.GetDevices", ex);
            }
            return null;
        }

        public static async Task<List<DeviceBase>> GetDevicesAsync(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Devices", conn);
            try
            {
                List<DeviceBase> ret = new List<DeviceBase>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "DeviceBase.GetDevicesAsync", ex);
            }
            return null;
        }

        public async Task SetLatestReceiver(SqlConnection conn, string externalReceiverId, DateTime lastReadingReceived)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("SetLatestReceiver", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@DeviceId", SqlDbType.Int).Value = this.DeviceId;
                cmd.Parameters.Add("@ExternalReceiverId", SqlDbType.VarChar, 70).Value = externalReceiverId;
                cmd.Parameters.Add("@LastReadingReceived", SqlDbType.DateTime).Value = lastReadingReceived;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Minor, "DeviceBase.SetLatestReceiver", ex);
            }
        }

        public async Task SetSensorUpdateInterval(SqlConnection conn, int sensorUpdateInterval)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("SetSensorUpdateInterval", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@DeviceId", SqlDbType.Int).Value = this.DeviceId;
                cmd.Parameters.Add("@SensorUpdateInterval", SqlDbType.Int).Value = sensorUpdateInterval;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Minor, "DeviceBase.SetSensorUpdateInterval", ex);
            }
        }

        private static DeviceBase InstantiateFromReader(SqlDataReader reader)
        {
            DeviceBase device = new DeviceBase()
            {
                DeviceId = SqlHelper.Read<int>(reader, "DeviceId"),
                Name = SqlHelper.Read<string>(reader, "Name"),
                Description = SqlHelper.Read<string>(reader, "Description"),
                LocationId = SqlHelper.Read<int>(reader, "LocationId"),
                IsActive = SqlHelper.Read<bool>(reader, "IsActive"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
                DeviceTypeId = SqlHelper.Read<int>(reader, "DeviceTypeId"),
                Version = SqlHelper.Read<int>(reader, "Version"),
                ExternalDeviceId = SqlHelper.Read<string>(reader, "ExternalDeviceId"),
                LatestReceiverId = SqlHelper.Read<string>(reader, "LatestReceiverId"),
                LastReadingReceived = SqlHelper.Read<DateTime?>(reader, "LastReadingReceived"),
                SensorUpdateInterval = SqlHelper.Read<int?>(reader, "SensorUpdateInterval"),
                Min = SqlHelper.Read<double?>(reader, "Min"),
                Max = SqlHelper.Read<double?>(reader, "Max"),
                MaxStDev = SqlHelper.Read<double?>(reader, "MaxStDev"),
                UsgsSiteId = SqlHelper.Read<int?>(reader, "UsgsSiteId"),
            };
            return device;
        }            

        private static string GetColumnList()
        {
            return "DeviceId, Name, Description, LocationId, IsActive, IsDeleted, DeviceTypeId, Version, ExternalDeviceId, LatestReceiverId, LastReadingReceived, SensorUpdateInterval, Min, Max, MaxStDev, UsgsSiteId";
        }

        public static async Task MarkDevicesAsUndeleted(SqlConnection conn, IEnumerable<int> deviceIds)
        {
            try
            {
                await SqlHelper.CallIdListProcedure(conn, "MarkDevicesAsUndeleted", deviceIds, 180);
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "DeviceBase.MarkDevicesAsUndeleted", ex);
            }
        }
    }
}
