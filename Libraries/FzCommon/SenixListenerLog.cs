using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class SenixListenerLog
    {
        public int? Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ListenerInfo { get; set; }
        public string ClientIP { get; set; }
        public string ExternalDeviceId { get; set; }
        public string ReceiverId { get; set; }
        public int? DeviceId { get; set; }
        public int? ReadingId { get; set; }
        public string RawSensorData { get; set; }
        public string Result { get; set; }

        public async Task Save(SqlConnection sqlConnection)
        {
            using (SqlCommand cmd = new SqlCommand("SaveSenixListenerLog", sqlConnection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (this.Id.HasValue && this.Id.Value > 0)
                {
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                }
                cmd.Parameters.Add("@Timestamp", SqlDbType.DateTime).Value = this.Timestamp;
                cmd.Parameters.Add("@ListenerInfo", SqlDbType.VarChar, 200).Value = this.ListenerInfo;
                cmd.Parameters.Add("@ClientIP", SqlDbType.VarChar, 64).Value = this.ClientIP;
                cmd.Parameters.Add("@RawSensorData", SqlDbType.Text).Value = this.RawSensorData;
                cmd.Parameters.Add("@Result", SqlDbType.VarChar, 200).Value = this.Result;
                if (!String.IsNullOrEmpty(this.ExternalDeviceId))
                {
                    cmd.Parameters.Add("@ExternalDeviceId", SqlDbType.VarChar, 70).Value = this.ExternalDeviceId;
                }
                if (!String.IsNullOrEmpty(this.ReceiverId))
                {
                    cmd.Parameters.Add("@ReceiverId", SqlDbType.VarChar, 70).Value = this.ReceiverId;
                }
                if (this.DeviceId.HasValue)
                {
                    cmd.Parameters.Add("@DeviceId", SqlDbType.Int).Value = this.DeviceId.Value;
                }
                if (this.ReadingId.HasValue)
                {
                    cmd.Parameters.Add("@ReadingId", SqlDbType.Int).Value = this.ReadingId.Value;
                }
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    if (!await dr.ReadAsync())
                    {
                        throw new ApplicationException("Error saving SenixListenerLog");
                    }
                    this.Id = SqlHelper.Read<int>(dr, "Id");
                }
            }
        }

        private static string GetColumnList()
        {
            return "Id, Timestamp, RawSensorData, Result, ListenerInfo, ClientIP, ExternalDeviceId, ReceiverId, DeviceId, ReadingId";
        }

        private static SenixListenerLog InstantiateFromReader(SqlDataReader reader)
        {
            SenixListenerLog log = new SenixListenerLog()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                Timestamp = SqlHelper.Read<DateTime>(reader, "Timestamp"),
                RawSensorData = SqlHelper.Read<string>(reader, "RawSensorData"),
                Result = SqlHelper.Read<string>(reader, "Result"),
                ListenerInfo = SqlHelper.Read<string>(reader, "ListenerInfo"),
                ClientIP = SqlHelper.Read<string>(reader, "ClientIP"),
                ExternalDeviceId = SqlHelper.Read<string>(reader, "ExternalDeviceId"),
                ReceiverId = SqlHelper.Read<string>(reader, "ReceiverId"),
                DeviceId = SqlHelper.Read<int?>(reader, "DeviceId"),
                ReadingId = SqlHelper.Read<int?>(reader, "ReadingId"),
            };
            return log;
        }

        public static async Task<SenixListenerLog> GetLogEntry(SqlConnection sqlcn, int id)
        {
            using (SqlCommand cmd = new SqlCommand("GetSenixListenerLogEntry", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        return InstantiateFromReader(dr);
                    }
                }
            }
            return null;
        }

        public static async Task<List<SenixListenerLog>> GetLogs(SqlConnection sqlcn, int? deviceId, DateTime? utcFromDate, DateTime? utcToDate)
        {
            List<SenixListenerLog> ret = new List<SenixListenerLog>();
            string proc = (deviceId.HasValue ? "GetSenixListenerLogsForDevice" : "GetSenixListenerLogsWithNoDevice");
            using (SqlCommand cmd = new SqlCommand(proc, sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (deviceId.HasValue) cmd.Parameters.Add("@DeviceId", SqlDbType.Int).Value = deviceId.Value;
                if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;

                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }

        public static async Task<List<SenixListenerLog>> GetAllLogs(SqlConnection sqlcn, DateTime? utcFromDate, DateTime? utcToDate)
        {
            List<SenixListenerLog> ret = new List<SenixListenerLog>();
            using (SqlCommand cmd = new SqlCommand("GetSenixListenerLogs", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (utcFromDate.HasValue) cmd.Parameters.Add("@fromTime", SqlDbType.DateTime).Value = utcFromDate;
                if (utcToDate.HasValue) cmd.Parameters.Add("@toTime", SqlDbType.DateTime).Value = utcToDate;

                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }

        //$ NOTE: This is temporary, for use with SenixLogReprocessor...
        public static List<SenixListenerLog> GetUnprocessedLogs(SqlConnection sqlcn)
        {
            List<SenixListenerLog> ret = new List<SenixListenerLog>();
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM SenixListenerLog WHERE ExternalDeviceId IS NULL", sqlcn);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ret.Add(InstantiateFromReader(reader));
                }
            }
            return ret;
        }
    }
}
