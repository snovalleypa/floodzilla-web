using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FzCommon;

namespace FloodzillaMonitor
{
    // This is a wrapper for all of the supported functions in this project.
    public class ProjectFunctions
    {
        public static async Task MonitorLocations()
        {
            LocationMonitor job = new();
            await job.Execute();
        }
        public static async Task SendDailyStatus()
        {
            DailyStatusJob job = new();
            await job.Execute();
        }
        public static async Task SendDailyForecast()
        {
            DailyForecastJob job = new();
            await job.Execute();
        }
    }

    // This is a set of wrappers that expose the functions either as TimerTrigger jobs in
    // the production Azure environment or as locally-callable HttpTrigger jobs for debugging.
    public class MonitorFunctions
    {
#if DEBUG
        [Function("MonitorLocations")]
        public static async Task<IActionResult> MonitorLocations([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.MonitorLocations();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("MonitorLocations")]
        // Current schedule is to run once at the top of every hour.
        public static async Task MonitorLocations([TimerTrigger("0 0 * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.MonitorLocations();
        }
#endif

#if DEBUG
        [Function("Trigger_SendDailyStatus")]
        public static async Task<IActionResult> Trigger_SendDailyStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.SendDailyStatus();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("SendDailyStatus")]
        // Run once a day overnight
        public static async Task SendDailyStatus([TimerTrigger("0 0 10 * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.SendDailyStatus();
        }
#endif

#if DEBUG
        [Function("SendDailyForecast")]
        public static async Task<IActionResult> SendDailyForecast([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.SendDailyForecast();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("SendDailyForecast")]
        // Run once a day overnight
        public static async Task SendDailyForecast([TimerTrigger("0 0 10 * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.SendDailyForecast();
        }
#endif
    }
}

