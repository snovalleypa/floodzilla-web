using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

using FzCommon.Fetchers;
using FzCommon.Processors;

namespace FloodzillaJobs
{
    public class WdfnUsgsDataFetcher : FloodzillaJob
    {
        readonly bool forceTestMode = false;
        public WdfnUsgsDataFetcher(bool forceTestMode) : base((forceTestMode ? "TEST_" : "") + "FloodzillaJob.WdfnUsgsDataFetcher",
                                                              (forceTestMode ? "TEST_" : "") + "USGS WDFN Reading Fetcher")
        {
            this.forceTestMode = forceTestMode;
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            await USGSWdfnFetcher.UpdateAllUsgsGauges(sqlcn, sbDetails, sbSummary, forceTestMode);
        }
    }
}
