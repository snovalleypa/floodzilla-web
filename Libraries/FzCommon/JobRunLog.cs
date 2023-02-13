using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class RecentJobRun
    {
        public int Id;
        public string JobName;
        public string? FriendlyName;
        public string MachineName;
        public DateTime StartTime;
        public DateTime EndTime;
        public string Summary;
        public string Exception;
        public string FullException;

        public static RecentJobRun InstantiateFromReader(SqlDataReader dr, string? columnPrefix = null)
        {
            if (columnPrefix == null)
            {
                columnPrefix = "";
            }
            return new RecentJobRun()
            {
                Id = (int)dr[columnPrefix + "Id"],
                JobName = (string)dr[columnPrefix + "JobName"],
                FriendlyName = SqlHelper.Read<string?>(dr, "FriendlyName"),
                MachineName = (string)dr[columnPrefix + "MachineName"],
                StartTime = (DateTime)dr[columnPrefix + "StartTime"],
                EndTime = (DateTime)dr[columnPrefix + "EndTime"],
                Summary = SqlHelper.Read<string>(dr, columnPrefix + "Summary"),
                Exception = SqlHelper.Read<string>(dr, columnPrefix + "Exception"),
                FullException = SqlHelper.Read<string>(dr, columnPrefix + "FullException"),
            };
        }
    }
    
    public class JobRunLog
    {
        internal JobRunLog(string jobName, DateTime startTime)
        {
            m_jobName = jobName;
            m_startTime = startTime;
        }

        internal RecentJobRun ReportJobRunSuccess(SqlConnection sqlcn, DateTime endTime)
        {
            return this.SaveRunLog(sqlcn, endTime);
        }

        internal RecentJobRun ReportJobRunException(SqlConnection sqlcn, Exception ex, DateTime exceptionTime)
        {
            ErrorManager.ReportException(ErrorSeverity.Major, m_jobName, ex, exceptionTime);
            return this.SaveRunLog(sqlcn, exceptionTime, ex);
        }

        private RecentJobRun SaveRunLog(SqlConnection sqlcn, DateTime endTime, Exception? ex = null)
        {
            SqlCommand cmd = new SqlCommand("SaveJobRunLog", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@JobName", m_jobName);
            cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName);
            cmd.Parameters.AddWithValue("@StartTime", m_startTime);
            cmd.Parameters.AddWithValue("@EndTime", endTime);
            cmd.Parameters.AddWithValue("@Summary", m_summary);
            if (ex != null)
            {
                cmd.Parameters.AddWithValue("@Exception", ex.Message);
                cmd.Parameters.AddWithValue("@FullException", ex.ToString());
            }
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (!dr.Read())
                {
                    throw new ApplicationException("Error saving LogBookEntry");
                }
                return RecentJobRun.InstantiateFromReader(dr);
            }
        }

        public static async Task<RecentJobRun> GetJobRun(SqlConnection sqlcn, int runId)
        {
            using (SqlCommand cmd = new SqlCommand("GetJobRunLog", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@runId", runId);
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        return RecentJobRun.InstantiateFromReader(dr);
                    }
                }
            }
            return null;
        }

        public static async Task<List<RecentJobRun>> GetRecentJobRuns(int runCount)
        {
            List<RecentJobRun> ret = new List<RecentJobRun>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                using (SqlCommand cmd = new SqlCommand("GetJobRunLogs", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@readingCount", runCount);
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (await dr.ReadAsync())
                        {
                            ret.Add(RecentJobRun.InstantiateFromReader(dr));
                        }
                    }
                }
            }
            return ret;
        }

        public static async Task<List<RecentJobRun>> GetLatestJobRunLogsAsync(SqlConnection sqlcn)
        {
            List<RecentJobRun> ret = new List<RecentJobRun>();
            using (SqlCommand cmd = new SqlCommand("GetLatestJobRunLogs", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(RecentJobRun.InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }
        
        public static async Task<List<RecentJobRun>> GetJobRunLogsForNameAsync(SqlConnection sqlcn, string jobName)
        {
            List<RecentJobRun> ret = new List<RecentJobRun>();
            using (SqlCommand cmd = new SqlCommand("GetJobRunLogsForName", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobName", jobName);
                using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(RecentJobRun.InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }

        public string Summary   { get { return m_summary; } set { m_summary = value; }}

        private string m_jobName;
        private DateTime m_startTime;
        private string m_summary;
    }
}
