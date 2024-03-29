using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public enum JobRunStatus
    {
        Success = 0,
        Disabled = 1,
        Error = 2,
    }

    public class JobEntryWithStatus
    {
        public JobEntry JobEntry;
        public RecentJobRun? RecentJobRun;
    }

    public class JobEntry : ILogTaggable
    {
        internal class JobEntryTaggableFactory : ILogBookTaggableFactory
        {
            public async Task<List<ILogTaggable>> GetAvailableTaggables(SqlConnection sqlcn, string category)
            {
                List<JobEntry> jobs = await JobEntry.GetAllJobs(sqlcn);
                List<ILogTaggable> ret = new List<ILogTaggable>();
                foreach (JobEntry job in jobs)
                {
                    ret.Add(job);
                }
                return ret;
            }
        }

        public const string TagCategory = "job";

#region ILogTaggable
        public string GetTagCategory() { return JobEntry.TagCategory; }
        public string GetTagId() { return this.JobName; }
        public string GetTagName() { return "Job: " + this.FriendlyName; }
#endregion

        public int Id                           { get; set; }
        public string JobName                   { get; set; }
        public string FriendlyName              { get; set; }
        public DateTime? LastStartTime          { get; set; }
        public DateTime? LastEndTime            { get; set; }
        public DateTime? LastSuccessfulEndTime  { get; set; }
        public JobRunStatus? LastRunStatus      { get; set; }
        public int? LastRunLogId                { get; set; }
        public string? LastRunSummary           { get; set; }
        public DateTime? LastErrorTime          { get; set; }
        public string? LastError                { get; set; }
        public string? LastFullException        { get; set; }
        public bool IsEnabled                   { get; set; }
        public string? DisableReason            { get; set; }
        public DateTime? DisabledTime           { get; set; }
        public string? DisabledBy               { get; set; }

        public static async Task<JobEntry> GetJob(SqlConnection sqlcn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT * FROM Jobs WHERE id = '{id}'", sqlcn);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "JobEntry.GetJob", ex);
            }
            return null;
        }

        public static async Task<JobEntry> EnsureJobEntry(SqlConnection sqlcn, string jobName, string friendlyName)
        {
            SqlCommand cmd = new SqlCommand("EnsureJob", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@JobName", jobName);
            cmd.Parameters.AddWithValue("@FriendlyName", friendlyName);
            using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
            {
                if (!await dr.ReadAsync())
                {
                    throw new ApplicationException("Error ensuring job entry");
                }
                return InstantiateFromReader(dr);
            }
        }

        public static async Task<List<JobEntry>> GetAllJobs(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("GetAllJobs", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            List<JobEntry> ret = new List<JobEntry>();
            using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
            {
                while (await dr.ReadAsync())
                {
                    ret.Add(InstantiateFromReader(dr));
                }
            }
            return ret;
        }

        public static async Task<List<JobEntryWithStatus>> GetAllJobsWithStatus(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("GetAllJobsWithCurrentStatus", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            List<JobEntryWithStatus> ret = new List<JobEntryWithStatus>();
            using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
            {
                while (await dr.ReadAsync())
                {
                    JobEntryWithStatus entry = new JobEntryWithStatus()
                    {
                        JobEntry = JobEntry.InstantiateFromReader(dr),
                    };
                    if (entry.JobEntry.LastRunLogId.HasValue && entry.JobEntry.LastRunLogId.Value > 0)
                    {
                        entry.RecentJobRun = RecentJobRun.InstantiateFromReader(dr, "JobRunLogs");
                    }
                    ret.Add(entry);
                }
            }
            return ret;
        }

        public bool StartJobRun(SqlConnection sqlcn)
        {
            this.LastStartTime = DateTime.UtcNow;
            if (!this.IsEnabled)
            {
                this.LastEndTime = this.LastStartTime;
                this.LastRunStatus = JobRunStatus.Disabled;
                this.Save(sqlcn);
                return false;
            }
            
            m_currentRun = new JobRunLog(this.JobName, this.LastStartTime.Value);

            //$ TODO: If we have any very-long-running jobs, we should have a status "Running",
            //$ and this function should save the current job entry.
            
            return true;
        }

        public void ReportJobSuccess(SqlConnection sqlcn, string summary)
        {
            if (this.m_currentRun == null)
            {
                throw new ApplicationException("ReportJobSuccess requires StartJobRun to be called first");
            }
            this.LastEndTime = DateTime.UtcNow;
            m_currentRun.Summary = summary;
            RecentJobRun jobRun = this.m_currentRun.ReportJobRunSuccess(sqlcn, this.LastEndTime.Value);
            this.LastRunLogId = jobRun.Id;
            this.m_currentRun = null;

            this.LastRunStatus = JobRunStatus.Success;
            this.LastSuccessfulEndTime = this.LastEndTime;
            this.LastRunSummary = summary;

            this.Save(sqlcn);
        }

        public void ReportJobException(SqlConnection sqlcn, Exception ex)
        {
            if (this.m_currentRun == null)
            {
                throw new ApplicationException("ReportJobException requires StartJobRun to be called first");
            }
            this.LastEndTime = DateTime.UtcNow;
            RecentJobRun jobRun = this.m_currentRun.ReportJobRunException(sqlcn, ex, this.LastEndTime.Value);
            this.LastRunLogId = jobRun.Id;
            this.m_currentRun = null;

            this.LastRunStatus = JobRunStatus.Error;
            this.LastRunSummary = null;
            this.LastErrorTime = this.LastEndTime;
            this.LastError = ex.Message;
            this.LastFullException = ex.ToString();
            this.Save(sqlcn);
        }

        public async Task DisableJob(SqlConnection sqlcn, string userName, string disableReason)
        {
            this.IsEnabled = false;
            this.DisabledTime = DateTime.UtcNow;
            this.DisabledBy = userName;
            this.DisableReason = disableReason;
            this.Save(sqlcn);
        }

        public async Task EnableJob(SqlConnection sqlcn)
        {
            this.IsEnabled = true;
            this.DisabledTime = null;
            this.DisabledBy = null;
            this.DisableReason = null;
            this.Save(sqlcn);
        }

        private JobEntry()
        {
            m_currentRun = null;
        }

        private void Save(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("SaveJob", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@JobName", this.JobName);
            cmd.Parameters.AddWithValue("@FriendlyName", this.FriendlyName);
            cmd.Parameters.AddWithValue("@IsEnabled", this.IsEnabled);
            SqlHelper.AddParamIfNotEmpty<DateTime?>(cmd.Parameters, "@LastStartTime", this.LastStartTime);
            SqlHelper.AddParamIfNotEmpty<DateTime?>(cmd.Parameters, "@LastEndTime", this.LastEndTime);
            SqlHelper.AddParamIfNotEmpty<DateTime?>(cmd.Parameters, "@LastSuccessfulEndTime", this.LastSuccessfulEndTime);
            SqlHelper.AddParamIfNotEmpty<JobRunStatus?>(cmd.Parameters, "@LastRunStatus", this.LastRunStatus);
            SqlHelper.AddParamIfNotEmpty<int?>(cmd.Parameters, "@LastRunLogId", this.LastRunLogId);
            SqlHelper.AddParamIfNotEmpty<string?>(cmd.Parameters, "@LastRunSummary", this.LastRunSummary);
            SqlHelper.AddParamIfNotEmpty<DateTime?>(cmd.Parameters, "@LastErrorTime", this.LastErrorTime);
            SqlHelper.AddParamIfNotEmpty<string?>(cmd.Parameters, "@LastError", this.LastError);
            SqlHelper.AddParamIfNotEmpty<string?>(cmd.Parameters, "@LastFullException", this.LastFullException);
            SqlHelper.AddParamIfNotEmpty<string?>(cmd.Parameters, "@DisableReason", this.DisableReason);
            SqlHelper.AddParamIfNotEmpty<DateTime?>(cmd.Parameters, "@DisabledTime", this.DisabledTime);
            SqlHelper.AddParamIfNotEmpty<string?>(cmd.Parameters, "@DisabledBy", this.DisabledBy);
            cmd.ExecuteNonQuery();
        }
        
        private static JobEntry InstantiateFromReader(SqlDataReader reader)
        {
            JobEntry jobEntry = new JobEntry()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                JobName = SqlHelper.Read<string>(reader, "JobName"),
                FriendlyName = SqlHelper.Read<string>(reader, "FriendlyName"),
                LastStartTime = SqlHelper.Read<DateTime?>(reader, "LastStartTime"),
                LastEndTime = SqlHelper.Read<DateTime?>(reader, "LastEndTime"),
                LastSuccessfulEndTime = SqlHelper.Read<DateTime?>(reader, "LastSuccessfulEndTime"),
                LastRunLogId = SqlHelper.Read<int?>(reader, "LastRunLogId"),
                LastRunSummary = SqlHelper.Read<string?>(reader, "LastRunSummary"),
                LastErrorTime = SqlHelper.Read<DateTime?>(reader, "LastErrorTime"),
                LastError = SqlHelper.Read<string?>(reader, "LastError"),
                LastFullException = SqlHelper.Read<string?>(reader, "LastFullException"),
                IsEnabled = SqlHelper.Read<bool>(reader, "IsEnabled"),
                DisableReason = SqlHelper.Read<string?>(reader, "DisableReason"),
                DisabledTime = SqlHelper.Read<DateTime?>(reader, "DisabledTime"),
                DisabledBy = SqlHelper.Read<string?>(reader, "DisabledBy"),
            };
            int? statusEnum = SqlHelper.Read<int?>(reader, "LastRunStatus");
            if (statusEnum.HasValue)
            {
                jobEntry.LastRunStatus = (JobRunStatus)statusEnum.Value;
            }
            return jobEntry;
        }

        private JobRunLog? m_currentRun = null;
    }
}
