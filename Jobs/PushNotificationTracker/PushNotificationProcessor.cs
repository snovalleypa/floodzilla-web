using Microsoft.Data.SqlClient;
using System.Text;

using FzCommon;
using FzCommon.Processors;

namespace PushNotificationTracker
{
    public class PushNotificationProcessor : FloodzillaJob
    {
        public PushNotificationProcessor() : base("FloodzillaJob.PushNotificationProcessor",
                                                  "Push Notification Processor")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            await PushNotificationManager.ProcessAttempts(sqlcn, sbDetails, sbSummary);
        }
    }
}
