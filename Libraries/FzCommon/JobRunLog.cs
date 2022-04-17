using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class RecentJobRun
    {
        public int Id;
        public string JobName;
        public string MachineName;
        public DateTime StartTime;
        public DateTime EndTime;
        public string Summary;
        public string Exception;
        public string FullException;

        public void InitializeFromReader(SqlDataReader dr)
        {
            Id = (int)dr["Id"];
            JobName = (string)dr["JobName"];
            MachineName = (string)dr["MachineName"];
            StartTime = (DateTime)dr["StartTime"];
            EndTime = (DateTime)dr["EndTime"];
            Summary = SqlHelper.Read<string>(dr, "Summary");
            Exception = SqlHelper.Read<string>(dr, "Exception");
            FullException = SqlHelper.Read<string>(dr, "FullException");
        }
    }
    
    public class JobRunLog
    {
        public JobRunLog(string jobName)
        {
            m_jobName = jobName;
            m_startTime = DateTime.UtcNow;
        }

        public void ReportJobRunSuccess()
        {
            this.SaveRunLog();
        }

        public void ReportJobRunException(Exception ex)
        {
            ErrorManager.ReportException(ErrorSeverity.Major, m_jobName, ex);
            this.SaveRunLog(ex);
        }

        private void SaveRunLog(Exception ex = null)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                SqlCommand cmd = new SqlCommand("SaveJobRunLog", sqlcn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobName", m_jobName);
                cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName);
                cmd.Parameters.AddWithValue("@StartTime", m_startTime);
                cmd.Parameters.AddWithValue("@EndTime", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@Summary", m_summary);
                if (ex != null)
                {
                    cmd.Parameters.AddWithValue("@Exception", ex.Message);
                    cmd.Parameters.AddWithValue("@FullException", ex.ToString());
                }
                else
                {
                }

                sqlcn.Open();
                cmd.ExecuteNonQuery();
                sqlcn.Close();
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
                        RecentJobRun rjr = new RecentJobRun();
                        rjr.InitializeFromReader(dr);
                        return rjr;
                    }
                }
            }
            return null;
        }

        public static async Task<List<string>> GetJobRunLogJobNamesAsync(SqlConnection sqlcn)
        {
            List<string> ret = new List<string>();
            using (SqlCommand cmd = new SqlCommand("GetJobRunLogJobNames", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(SqlHelper.Read<string>(dr, "JobName"));
                    }
                }
            }
            return ret;
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
                            RecentJobRun rjr = new RecentJobRun();
                            rjr.InitializeFromReader(dr);
                            ret.Add(rjr);
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
                        RecentJobRun rjr = new RecentJobRun();
                        rjr.InitializeFromReader(dr);
                        ret.Add(rjr);
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
                        RecentJobRun rjr = new RecentJobRun();
                        rjr.InitializeFromReader(dr);
                        ret.Add(rjr);
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
