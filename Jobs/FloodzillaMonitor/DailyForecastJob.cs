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

namespace FloodzillaMonitor
{

    public class DailyForecastJob : FloodzillaJob
    {
        public DailyForecastJob() : base("FloodzillaMonitor.SendDailyForecast",
                                         "Daily NOAA Forecast Email")
        {
        }

        protected override async Task RunJob(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            NoaaForecastSet previous = await NoaaForecastSet.GetPreviousForecastSet(sqlcn);
            NoaaForecastSet current = await NoaaForecastSet.GetLatestForecastSet(sqlcn);
            ForecastEmailModel model = await NoaaForecastProcessor.BuildEmailModel(sqlcn, current, previous);
            model.Context = ForecastEmailModel.ForecastContext.Daily;

            //$ TODO: Do this for all regions
            List<UserBase> users = await UserBase.GetUsersForNotifyDailyForecasts(sqlcn);
            await m_notificationManager.NotifyUserList(sqlcn,
                                                       model,
                                                       FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                       users,
                                                       true,
                                                       false,
                                                       false,
                                                       sbSummary,
                                                       sbDetails);
        }
    }
}
