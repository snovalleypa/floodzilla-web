using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using FzCommon;

[assembly: FunctionsStartup(typeof(FloodZillaMonitor.MonitorFunctions))]

namespace FloodZillaMonitor
{
    public class MonitorFunctions : FunctionsStartup
    {

        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();

            //$ TODO: Any other configuration/initialization?
        }

#if DEBUG
        [FunctionName("MonitorLocations")]
        public static async Task<IActionResult> MonitorLocations([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await LocationMonitor.MonitorLocations(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("MonitorLocations")]
        // Current schedule is to run once at the top of every hour.
        public static async Task MonitorLocations([TimerTrigger("0 0 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await LocationMonitor.MonitorLocations(log);
        }
#endif

#if DEBUG
        [FunctionName("SendDailyStatus")]
        public static async Task<IActionResult> SendDailyStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await DailyStatusJob.SendDailyStatus(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("SendDailyStatus")]
        // Run once a day overnight
        public static async Task SendDailyStatus([TimerTrigger("0 0 10 * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await DailyStatusJob.SendDailyStatus(log);
        }
#endif

#if DEBUG
        [FunctionName("SendDailyForecast")]
        public static async Task<IActionResult> SendDailyForecast([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await DailyForecastJob.SendDailyForecast(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("SendDailyForecast")]
        // Run once a day overnight
        public static async Task SendDailyForecast([TimerTrigger("0 0 10 * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await DailyForecastJob.SendDailyForecast(log);
        }
#endif
    }

}
