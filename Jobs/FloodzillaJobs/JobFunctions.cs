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

[assembly: FunctionsStartup(typeof(FloodzillaJob.JobFunctions))]

namespace FloodzillaJob
{
    public class JobFunctions : FunctionsStartup
    {

        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();

            //$ TODO: Any other configuration/initialization?
        }

#if DEBUG
        [FunctionName("CalculateUsgsLevels")]
        public static async Task<IActionResult> CalculateUsgsLevels([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await UsgsDataSource.CalculateUsgsLevels(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("CalculateUsgsLevels")]
        // Current schedule is to run once per minute
        public static async Task CalculateUsgsLevels([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await UsgsDataSource.CalculateUsgsLevels(log);
        }
#endif

#if DEBUG
        [FunctionName("FetchUsgsReadings")]
        public static async Task<IActionResult> FetchUsgsReadings([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await UsgsDataFetcher.FetchUsgsReadings(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("FetchUsgsReadings")]
        // Current schedule is to run once every 5 minutes.
        public static async Task FetchUsgsReadings([TimerTrigger("0 */5 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await UsgsDataFetcher.FetchUsgsReadings(log);
        }
#endif

#if DEBUG
        [FunctionName("FetchNoaaForecasts")]
        public static async Task<IActionResult> FetchNoaaForecasts([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await NoaaForecastFetcher.FetchNoaaForecasts(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("FetchNoaaForecasts")]
        // Current schedule is to run once every 15 minutes.
        public static async Task FetchNoaaForecasts([TimerTrigger("0 */15 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await NoaaForecastFetcher.FetchNoaaForecasts(log);
        }
#endif

#if DEBUG
        [FunctionName("CollectGageStatistics")]
        public static async Task<IActionResult> CollectGageStatistics([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await GageStatisticsCollector.CollectGageStatistics(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("CollectGageStatistics")]
        // Current schedule is to run once per day.  Statistics are collected based on region time, so run early morning per-region.
        public static async Task CollectGageStatistics([TimerTrigger("0 0 9 * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await GageStatisticsCollector.CollectGageStatistics(log);
        }
#endif
        
#if DEBUG
        [FunctionName("DetectGageEvents")]
        public static async Task<IActionResult> DetectGageEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await GageEventDetector.DetectGageEvents(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("DetectGageEvents")]
        // Current schedule is to run once per minute.
        public static async Task DetectGageEvents([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await GageEventDetector.DetectGageEvents(log);
        }
#endif
        
#if DEBUG
        [FunctionName("NotifyGageEvents")]
        public static async Task<IActionResult> NotifyGageEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await GageEventNotifier.NotifyGageEvents(log);
            return new OkObjectResult("Triggered");
        }
#else
        [FunctionName("NotifyGageEvents")]
        // Current schedule is to run once per minute.
        public static async Task NotifyGageEvents([TimerTrigger("0 */1 * * * *", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            await GageEventNotifier.NotifyGageEvents(log);
        }
#endif
        
#if DEBUG
        [FunctionName("TestUsgsCalculate")]
        public static async Task<IActionResult> TestUsgsCalculate([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            await UsgsDataSource.TestUsgsCalculate(log);
            return new OkObjectResult("Triggered");
        }
#endif
    }

}
