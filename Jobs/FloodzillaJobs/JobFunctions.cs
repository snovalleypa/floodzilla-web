using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace FloodzillaJobs
{
    // This is a wrapper for all of the supported functions in this project.
    public class ProjectFunctions
    {
        public static async Task TESTMODE_FetchWdfnUsgsReadings()
        {
            WdfnUsgsDataFetcher job = new WdfnUsgsDataFetcher(true);
            await job.Execute();
        }
        public static async Task FetchWdfnUsgsReadings()
        {
            WdfnUsgsDataFetcher job = new WdfnUsgsDataFetcher(false);
            await job.Execute();
        }
        public static async Task FetchNoaaForecasts()
        {
            NoaaForecastFetcher job = new NoaaForecastFetcher();
            await job.Execute();
        }
        public static async Task CollectGageStatistics()
        {
            GageStatisticsCollector job = new GageStatisticsCollector();
            await job.Execute();
        }
        public static async Task DetectGageEvents()
        {
            GageEventDetector job = new GageEventDetector();
            await job.Execute();
        }
        public static async Task NotifyGageEvents()
        {
            GageEventNotifier job = new GageEventNotifier();
            await job.Execute();
        }
    }

    public class JobFunctions
    {
        // This is currently only enabled for manual calling.
#if DEBUG
        [Function("TESTMODE_FetchWdfnUsgsReadings")]
        public static async Task<IActionResult> TESTMODE_FetchWdfnUsgsReadings([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.TESTMODE_FetchWdfnUsgsReadings();
            return new OkObjectResult("Triggered");
        }
#endif

#if DEBUG
        [Function("FetchWdfnUsgsReadings")]
        public static async Task<IActionResult> FetchWdfnUsgsReadings([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.FetchWdfnUsgsReadings();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("FetchWdfnUsgsReadings")]
        // Current schedule is to run once every 5 minutes.
        public static async Task FetchWdfnUsgsReadings([TimerTrigger("0 */5 * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.FetchWdfnUsgsReadings();
        }
#endif

#if DEBUG
        [Function("FetchNoaaForecasts")]
        public static async Task<IActionResult> FetchNoaaForecasts([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.FetchNoaaForecasts();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("FetchNoaaForecasts")]
        // Current schedule is to run once every 15 minutes.
        public static async Task FetchNoaaForecasts([TimerTrigger("0 */15 * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.FetchNoaaForecasts();
        }
#endif

#if DEBUG
        [Function("CollectGageStatistics")]
        public static async Task<IActionResult> CollectGageStatistics([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.CollectGageStatistics();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("CollectGageStatistics")]
        // Current schedule is to run once per day.  Statistics are collected based on region time, so run early morning per-region.
        public static async Task CollectGageStatistics([TimerTrigger("0 0 9 * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.CollectGageStatistics();
        }
#endif

#if DEBUG
        [Function("DetectGageEvents")]
        public static async Task<IActionResult> DetectGageEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.DetectGageEvents();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("DetectGageEvents")]
        // Current schedule is to run once per minute.
        public static async Task DetectGageEvents([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.DetectGageEvents();
        }
#endif

#if DEBUG
        [Function("NotifyGageEvents")]
        public static async Task<IActionResult> NotifyGageEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await ProjectFunctions.NotifyGageEvents();
            return new OkObjectResult("Triggered");
        }
#else
        [Function("NotifyGageEvents")]
        // Current schedule is to run once per minute.
        public static async Task NotifyGageEvents([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.NotifyGageEvents();
        }
#endif
    }
}
