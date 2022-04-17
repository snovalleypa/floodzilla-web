using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;
using FzCommon.Processors;

namespace FloodZillaMonitor
{
    public class DailyForecastJob
    {
        public static async Task SendDailyForecast(ILogger log)
        {
            JobRunLog runLog = new JobRunLog("FloodZillaMonitor.SendDailyForecast");
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    sqlcn.Open();
                    NoaaForecastSet previous = await NoaaForecastSet.GetPreviousForecastSet(sqlcn);
                    NoaaForecastSet current = await NoaaForecastSet.GetLatestForecastSet(sqlcn);
                    ForecastEmailModel model = await NoaaForecastProcessor.BuildEmailModel(sqlcn, current, previous);
                    model.Context = ForecastEmailModel.ForecastContext.Daily;

                    StringBuilder sbDetails = new StringBuilder();
                    StringBuilder sbResult = new StringBuilder();

                    //$ TODO: Do this for all regions
                    List<UserBase> users = await UserBase.GetUsersForNotifyDailyForecasts(sqlcn);
                    await model.SendEmailToUserList(sqlcn,
                                                    FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                    users,
                                                    false,
                                                    sbResult,
                                                    sbDetails);
                    runLog.Summary = sbResult.ToString();
                    runLog.ReportJobRunSuccess();
                }
            }
            catch (Exception ex)
            {
                runLog.ReportJobRunException(ex);
                throw;
            }
        }
    }
}
