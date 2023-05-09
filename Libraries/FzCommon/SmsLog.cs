using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class SmsLog
    {
        public int Id;
        public DateTime Timestamp;
        public string MachineName;
        public string LogEntryType;
        public string? FromNumber;
        public string? ToNumber;
        public string? Text;
        public string? Result;
        public string? Details;

        public SmsLog()
        {
            MachineName = "N/A";
            LogEntryType = "N/A";
        }

        public static async Task<SmsLog> Create(SqlConnection sqlcn,
                                                DateTime timestamp,
                                                string machineName,
                                                string logEntryType,
                                                string? fromNumber,
                                                string? toNumber,
                                                string? text)
        {
            using SqlCommand cmd = new SqlCommand("CreateSmsLog", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@MachineName", machineName);
            cmd.Parameters.AddWithValue("@LogEntryType", logEntryType);
            if (fromNumber != null)
            {
                cmd.Parameters.AddWithValue("@FromNumber", fromNumber);
            }
            if (toNumber != null)
            {
                cmd.Parameters.AddWithValue("@ToNumber", toNumber);
            }
            if (text != null)
            {
                cmd.Parameters.AddWithValue("@Text", text);
            }
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
            {
                throw new ApplicationException("Error saving SMS log");
            }
            return InstantiateFromReader(dr);
        }

        public async Task UpdateStatus(SqlConnection sqlcn, string result, string? details)
        {
            using SqlCommand cmd = new SqlCommand("SetSmsLogResult", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@Result", result);
            if (details != null)
            {
                cmd.Parameters.AddWithValue("@Details", details);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        private static SmsLog InstantiateFromReader(SqlDataReader dr)
        {
            return new SmsLog()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
                MachineName = SqlHelper.Read<string>(dr, "MachineName"),
                LogEntryType = SqlHelper.Read<string>(dr, "LogEntryType"),
                FromNumber = SqlHelper.Read<string?>(dr, "FromNumber"),
                ToNumber = SqlHelper.Read<string?>(dr, "ToNumber"),
                Text = SqlHelper.Read<string?>(dr, "Text"),
                Result = SqlHelper.Read<string?>(dr, "Result"),
                Details = SqlHelper.Read<string?>(dr, "Details"),
            };
        }
    }
}
