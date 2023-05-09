using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class PushDeviceLog
    {
        public const string EntryType_Registered = "Registered";
        public const string EntryType_Removed = "Removed";
        
        public int Id;
        public DateTime Timestamp;
        public string MachineName;
        public string LogEntryType;
        public string? Token;
        public int? UserId;
        public string? Platform;
        public string? Language;
        public string? Extra;

         public PushDeviceLog()
        {
            MachineName = "N/A";
            LogEntryType = "N/A";
        }

        public static async Task<PushDeviceLog> Create(SqlConnection sqlcn,
                                                       DateTime timestamp,
                                                       string machineName,
                                                       string logEntryType,
                                                       string? token,
                                                       int? userId,
                                                       string? platform,
                                                       string? language,
                                                       string? extra)
        {
            using SqlCommand cmd = new SqlCommand("CreatePushDeviceLog", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@MachineName", machineName);
            cmd.Parameters.AddWithValue("@LogEntryType", logEntryType);
            if (!String.IsNullOrEmpty(token))
            {
                cmd.Parameters.AddWithValue("@Token", token);
            }
            if (userId != null)
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
            }
            if (!String.IsNullOrEmpty(platform))
            {
                cmd.Parameters.AddWithValue("@Platform", platform);
            }
            if (!String.IsNullOrEmpty(language))
            {
                cmd.Parameters.AddWithValue("@Language", language);
            }
            if (!String.IsNullOrEmpty(extra))
            {
                cmd.Parameters.AddWithValue("@Extra", extra);
            }
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
            {
                throw new ApplicationException("Error saving push device log");
            }
            return InstantiateFromReader(dr);
        }

        private static PushDeviceLog InstantiateFromReader(SqlDataReader dr)
        {
            return new PushDeviceLog()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
                MachineName = SqlHelper.Read<string>(dr, "MachineName"),
                LogEntryType = SqlHelper.Read<string>(dr, "LogEntryType"),
                Token = SqlHelper.Read<string?>(dr, "Token"),
                UserId = SqlHelper.Read<int?>(dr, "UserId"),
                Platform = SqlHelper.Read<string?>(dr, "Platform"),
                Language = SqlHelper.Read<string?>(dr, "Language"),
                Extra = SqlHelper.Read<string?>(dr, "Extra"),
            };
        }
    }
}
