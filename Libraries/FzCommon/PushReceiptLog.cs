using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class PushReceiptLog
    {
        public int Id;
        public DateTime Timestamp;
        public string MachineName;
        public string LogEntryType;
        public string TicketIds;
        public string? Result;
        public string? Response;

        public PushReceiptLog()
        {
            MachineName = "N/A";
            LogEntryType = "N/A";
            TicketIds = "N/A";
        }

        public static PushReceiptLog InstantiateFromReader(SqlDataReader dr)
        {
            return new PushReceiptLog()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
                MachineName = SqlHelper.Read<string>(dr, "MachineName"),
                LogEntryType = SqlHelper.Read<string>(dr, "LogEntryType"),
                TicketIds = SqlHelper.Read<string>(dr, "TicketIds"),
                Result = SqlHelper.Read<string?>(dr, "Result"),
                Response = SqlHelper.Read<string?>(dr, "Response"),
            };
        }

        public static async Task<PushReceiptLog> Create(SqlConnection sqlcn,
                                                             DateTime timestamp,
                                                             string machineName,
                                                             string logEntryType,
                                                             string ticketIds)
        {
            using SqlCommand cmd = new SqlCommand("CreatePushReceiptLog", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@MachineName", machineName);
            cmd.Parameters.AddWithValue("@LogEntryType", logEntryType);
            cmd.Parameters.AddWithValue("@TicketIds", ticketIds);
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
            {
                throw new ApplicationException("Error saving push receipt log");
            }
            return InstantiateFromReader(dr);
        }

        public async Task UpdateStatus(SqlConnection sqlcn, string result, string? response)
        {
            using SqlCommand cmd = new SqlCommand("SetPushReceiptLogResult", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@Result", result);
            if (response != null)
            {
                cmd.Parameters.AddWithValue("@Response", response);
            }
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
