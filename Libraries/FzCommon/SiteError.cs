using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class SiteError
    {
        public DateTime Timestamp;
        public string Severity;
        public string Source;
        public string Error;

        public static void SaveSiteError(SqlConnection sqlcn,
                                         DateTime timestamp,
                                         string severity,
                                         string source,
                                         string error)
        {
            using (SqlCommand cmd = new SqlCommand("SaveSiteError", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Timestamp", timestamp);
                cmd.Parameters.AddWithValue("@Severity", severity);
                cmd.Parameters.AddWithValue("@Source", source);
                cmd.Parameters.AddWithValue("@Error", error);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

