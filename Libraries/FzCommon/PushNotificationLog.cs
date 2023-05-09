using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class PushNotificationLog
    {
        public int Id;
        public DateTime Timestamp;
        public string MachineName;
        public string LogEntryType;
        public string Tokens;
        public string? Title;
        public string? Subtitle;
        public string? Body;
        public string? Data;
        public string? Result;
        public string? Response;

        public PushNotificationLog()
        {
            MachineName = "N/A";
            LogEntryType = "N/A";
            Tokens = "N/A";
        }

        public static async Task<PushNotificationLog> Create(SqlConnection sqlcn,
                                                             DateTime timestamp,
                                                             string machineName,
                                                             string logEntryType,
                                                             string tokens,
                                                             string? title,
                                                             string? subtitle,
                                                             string? body,
                                                             string? data)
        {
            using SqlCommand cmd = new SqlCommand("CreatePushNotificationLog", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@MachineName", machineName);
            cmd.Parameters.AddWithValue("@LogEntryType", logEntryType);
            cmd.Parameters.AddWithValue("@Tokens", tokens);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Subtitle", subtitle);
            cmd.Parameters.AddWithValue("@Body", body);
            cmd.Parameters.AddWithValue("@Data", data);
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
            {
                throw new ApplicationException("Error saving push notification log");
            }
            return InstantiateFromReader(dr);
        }

        public async Task UpdateStatus(SqlConnection sqlcn, string result, string? response)
        {
            using SqlCommand cmd = new SqlCommand("SetPushNotificationLogResult", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@Result", result);
            if (response != null)
            {
                cmd.Parameters.AddWithValue("@Response", response);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        private static PushNotificationLog InstantiateFromReader(SqlDataReader dr)
        {
            return new PushNotificationLog()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
                MachineName = SqlHelper.Read<string>(dr, "MachineName"),
                LogEntryType = SqlHelper.Read<string>(dr, "LogEntryType"),
                Tokens = SqlHelper.Read<string>(dr, "Tokens"),
                Title = SqlHelper.Read<string?>(dr, "Title"),
                Subtitle = SqlHelper.Read<string?>(dr, "Subtitle"),
                Body = SqlHelper.Read<string?>(dr, "Body"),
                Data = SqlHelper.Read<string?>(dr, "Data"),
                Result = SqlHelper.Read<string?>(dr, "Result"),
                Response = SqlHelper.Read<string?>(dr, "Response"),
            };
        }
    }
}
