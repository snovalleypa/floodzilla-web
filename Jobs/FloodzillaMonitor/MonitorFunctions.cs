using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using FzCommon;

[assembly: FunctionsStartup(typeof(FloodzillaMonitor.MonitorFunctions))]
namespace FloodzillaMonitor
{
    // This is a wrapper for all of the supported functions in this project.
    public class ProjectFunctions
    {
        public static async Task MonitorLocations()
        {
            LocationMonitor job = new LocationMonitor();
            await job.Execute();
        }
        public static async Task SendDailyStatus()
        {
            DailyStatusJob job = new DailyStatusJob();
            await job.Execute();
        }
        public static async Task SendDailyForecast()
        {
            DailyForecastJob job = new DailyForecastJob();
            await job.Execute();
        }
    }

    // This is a set of wrappers that expose the functions either as TimerTrigger jobs in
    // the production Azure environment or as locally-callable HttpTrigger jobs for debugging.
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
            await ProjectFunctions.MonitorLocations();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("MonitorLocations")]
        // Current schedule is to run once at the top of every hour.
        public static async Task MonitorLocations([TimerTrigger("0 0 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.MonitorLocations();
        }
#endif

#if DEBUG
        [FunctionName("SendDailyStatus")]
        public static async Task<IActionResult> SendDailyStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.SendDailyStatus();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("SendDailyStatus")]
        // Run once a day overnight
        public static async Task SendDailyStatus([TimerTrigger("0 0 10 * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.SendDailyStatus();
        }
#endif

#if DEBUG
        [FunctionName("SendDailyForecast")]
        public static async Task<IActionResult> SendDailyForecast([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.SendDailyForecast();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("SendDailyForecast")]
        // Run once a day overnight
        public static async Task SendDailyForecast([TimerTrigger("0 0 10 * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.SendDailyForecast();
        }
#endif
    }
}

