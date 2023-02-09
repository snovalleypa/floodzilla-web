using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace FzCommon.Processors
{
    public abstract class FloodzillaJob
    {
        public FloodzillaJob(string jobName, string friendlyName)
        {
            this.m_jobName = jobName;
            this.m_friendlyName = friendlyName;
        }

        public async Task Execute()
        {
            // Try to do basic initialization.  If this doesn't work, we won't be able to do
            // any real work, so just notify appropriately and exit.
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();

                    JobEntry jobEntry = await JobEntry.EnsureJobEntry(sqlcn, this.m_jobName, this.m_friendlyName);
                    if (jobEntry.StartJobRun(sqlcn))
                    {
                        await this.ExecuteJob(sqlcn, jobEntry);
                    }
                    await sqlcn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                // Pass this off to ErrorManager.  We probably won't be able to save a run log
                // or anything, but hopefully we can do the other kinds of error reporting.
                ErrorManager.ReportException(ErrorSeverity.Major, this.m_jobName, ex);
            }
        }

        private async Task ExecuteJob(SqlConnection sqlcn, JobEntry jobEntry)
        {
            StringBuilder sbDetails = new StringBuilder();
            StringBuilder sbSummary = new StringBuilder();

            try
            {
                await this.RunJob(sqlcn, sbDetails, sbSummary);
                jobEntry.ReportJobSuccess(sqlcn, sbSummary.ToString());

                // If this fails, just eat the exception -- this is just informational, so we
                // can lose it without worrying about it.
                try
                {
                    await AzureJobHelpers.SaveJobDetailedStatus(this.m_jobName, sbDetails.ToString());
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                jobEntry.ReportJobException(sqlcn, ex);
            }
        }

        //$ TODO: Instead of passing StringBuilders around, make a Logger-style interface.
        protected abstract Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary);

        private string m_jobName;
        private string m_friendlyName;
    }
}

