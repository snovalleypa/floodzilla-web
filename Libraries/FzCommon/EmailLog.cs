using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class EmailLog
    {
        public int Id;
        public DateTime Timestamp;
        public string MachineName;
        public string FromAddress;
        public string ToAddress;
        public string Subject;
        public string Text;
        public bool TextIsHtml;
        public string? Result;
        public string? Details;

        public EmailLog()
        {
            MachineName = "N/A";
        }

        public static async Task<EmailLog> Create(SqlConnection sqlcn,
                                                  DateTime timestamp,
                                                  string machineName,
                                                  string fromAddress,
                                                  string toAddress,
                                                  string subject,
                                                  string text,
                                                  bool textIsHtml)
        {
            using SqlCommand cmd = new SqlCommand("CreateEmailLog", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@MachineName", machineName);
            cmd.Parameters.AddWithValue("@FromAddress", fromAddress);
            cmd.Parameters.AddWithValue("@ToAddress", toAddress);
            cmd.Parameters.AddWithValue("@Subject", subject);
            cmd.Parameters.AddWithValue("@Text", text);
            cmd.Parameters.AddWithValue("@TextIsHtml", textIsHtml);
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
            {
                throw new ApplicationException("Error saving email log");
            }
            return InstantiateFromReader(dr);
        }

        public async Task UpdateStatus(SqlConnection sqlcn, string result, string? details)
        {
            using SqlCommand cmd = new SqlCommand("SetEmailLogResult", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@Result", result);
            if (details != null)
            {
                cmd.Parameters.AddWithValue("@Details", details);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        private static EmailLog InstantiateFromReader(SqlDataReader dr)
        {
            return new EmailLog()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
                MachineName = SqlHelper.Read<string>(dr, "MachineName"),
                FromAddress = SqlHelper.Read<string>(dr, "FromAddress"),
                ToAddress = SqlHelper.Read<string>(dr, "ToAddress"),
                Subject = SqlHelper.Read<string>(dr, "Subject"),
                Text = SqlHelper.Read<string>(dr, "Text"),
                TextIsHtml = SqlHelper.Read<bool>(dr, "TextIsHtml"),
                Result = SqlHelper.Read<string?>(dr, "Result"),
                Details = SqlHelper.Read<string?>(dr, "Details"),
            };
        }
    }
}
