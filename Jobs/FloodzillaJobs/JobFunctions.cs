using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using FzCommon;

[assembly: FunctionsStartup(typeof(FloodzillaJobs.JobFunctions))]

namespace FloodzillaJobs
{
    // This is a wrapper for all of the supported functions in this project.
    public class ProjectFunctions
    {
#if THIS_IS_OBSOLETE
        public static async Task CalculateUsgsLevels()
        {
            UsgsLevelCalculator job = new UsgsLevelCalculator();
            await job.Execute();
        }
#endif
        public static async Task FetchUsgsReadings()
        {
            UsgsDataFetcher job = new UsgsDataFetcher();
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

    public class JobFunctions : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();
            //$ TODO: Any other configuration/initialization?
        }

#if DEBUG
        [FunctionName("FetchUsgsReadings")]
        public static async Task<IActionResult> FetchUsgsReadings([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.FetchUsgsReadings();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("FetchUsgsReadings")]
        // Current schedule is to run once every 5 minutes.
        public static async Task FetchUsgsReadings([TimerTrigger("0 */5 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.FetchUsgsReadings();
        }
#endif

#if DEBUG
        [FunctionName("FetchNoaaForecasts")]
        public static async Task<IActionResult> FetchNoaaForecasts([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.FetchNoaaForecasts();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("FetchNoaaForecasts")]
        // Current schedule is to run once every 15 minutes.
        public static async Task FetchNoaaForecasts([TimerTrigger("0 */15 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.FetchNoaaForecasts();
        }
#endif

#if DEBUG
        [FunctionName("CollectGageStatistics")]
        public static async Task<IActionResult> CollectGageStatistics([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.CollectGageStatistics();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("CollectGageStatistics")]
        // Current schedule is to run once per day.  Statistics are collected based on region time, so run early morning per-region.
        public static async Task CollectGageStatistics([TimerTrigger("0 0 9 * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.CollectGageStatistics();
        }
#endif
        
#if DEBUG
        [FunctionName("DetectGageEvents")]
        public static async Task<IActionResult> DetectGageEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.DetectGageEvents();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("DetectGageEvents")]
        // Current schedule is to run once per minute.
        public static async Task DetectGageEvents([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.DetectGageEvents();
        }
#endif
        
#if DEBUG
        [FunctionName("NotifyGageEvents")]
        public static async Task<IActionResult> NotifyGageEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await ProjectFunctions.NotifyGageEvents();
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("NotifyGageEvents")]
        // Current schedule is to run once per minute.
        public static async Task NotifyGageEvents([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await ProjectFunctions.NotifyGageEvents();
        }
#endif
    }

}
