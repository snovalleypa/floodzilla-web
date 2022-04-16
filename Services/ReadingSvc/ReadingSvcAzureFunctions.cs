using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;

// Azure functions wrapper for ReadingSvc.

[assembly: FunctionsStartup(typeof(ReadingSvc.ReadingSvcAzureFunctions))]
namespace ReadingSvc
{
    public class ReadingSvcAzureFunctions : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            ReadingSvc.Initialize();
        }

        // Query parameters:
        // int regionId (optional, defaults to The One True Region)
        // string id -- either an integer Id or a string PublicLocationId
        // DateTime fromDateTime (optional) -- expected to be in region time
        // DateTime toDateTime (optional) -- expected to be in region time
        // bool showDeletedReadings (optional, default false)
        // bool showMissingReadings (optional, default false)
        // bool getMinimalReadings (optional, default false)
        // bool includeStatus (optional, default false)
        // bool includePredictions (optional, default false)
        // bool includeForecast (optional, default false)
        // int lastReadingId (optional, default 0)
        // bool returnUtc - (optional, default false) if true, all reading timestamps will be returned as UTC; otherwise region time will be used.
        [FunctionName("GetGageReadings")]
        public static async Task<IActionResult> GetGageReadings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                int regionId = FzCommon.Constants.SvpaRegionId;
                string q = req.Query["regionId"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Int32.TryParse(q, out regionId))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid region id '{0}'", q));
                    }
                }
                string id = req.Query["id"];
                if (String.IsNullOrEmpty(id))
                {
                    return new BadRequestObjectResult("Missing required parameter 'id'");
                }
                DateTime? fromDateTime = null;
                q = req.Query["fromDateTime"];
                if (!String.IsNullOrEmpty(q))
                {
                    DateTime tmp;
                    if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid from date '{0}'", q));
                    }
                    fromDateTime = tmp;
                }
                DateTime? toDateTime = null;
                q = req.Query["toDateTime"];
                if (!String.IsNullOrEmpty(q))
                {
                    DateTime tmp;
                    if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid to date '{0}'", q));
                    }
                    toDateTime = tmp;
                }
                bool showDeletedReadings = false;
                q = req.Query["showDeletedReadings"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out showDeletedReadings))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid showDeletedReadings value '{0}'", q));
                    }
                }
                bool showMissingReadings = false;
                q = req.Query["showMissingReadings"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out showMissingReadings))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid showMissingReadings value '{0}'", q));
                    }
                }
                bool getMinimalReadings = false;
                q = req.Query["getMinimalReadings"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out getMinimalReadings))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid getMinimalReadings value '{0}'", q));
                    }
                }
                bool includeStatus = false;
                q = req.Query["includeStatus"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out includeStatus))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid includeStatus value '{0}'", q));
                    }
                }
                bool includePredictions = false;
                q = req.Query["includePredictions"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out includePredictions))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid includePredictions value '{0}'", q));
                    }
                }
                bool includeForecast = false;
                q = req.Query["includeForecast"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out includeForecast))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid includeForecast value '{0}'", q));
                    }
                }
                int lastReadingId = 0;
                q = req.Query["lastReadingId"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Int32.TryParse(q, out lastReadingId))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid lastReadingId value '{0}'", q));
                    }
                }
                bool returnUtc = false;
                q = req.Query["returnUtc"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out returnUtc))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid returnUtc value '{0}'", q));
                    }
                }

                ReadingStoreResponse resp
                        = await ReadingSvc.GetGageReadings(regionId,
                                                           id,
                                                           fromDateTime,
                                                           toDateTime,
                                                           showDeletedReadings,
                                                           showMissingReadings,
                                                           getMinimalReadings,
                                                           includeStatus,
                                                           includePredictions,
                                                           includeForecast,
                                                           lastReadingId,
                                                           returnUtc);

                // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
                // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
                // quoting the way OkObjectResult does.
                return new ContentResult() { Content = JsonConvert.SerializeObject(resp), ContentType = "application/json" };
            }
            catch (Exception e)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReadingSvc.GetGageReadings", e);
                throw;
            }
        }

        // Query parameters:
        // int regionId (optional, defaults to The One True Region)
        // string id -- either an integer Id or a string PublicLocationId
        // DateTime fromDateTime (optional)
        // DateTime toDateTime (optional)
        // bool showDeletedReadings (optional, default false)
        // bool showMissingReadings (optional, default false)
        // bool getMinimalReadings (optional, default false)
        // bool includeStatus (optional, default false)
        // bool includePredictions (optional, default false);
        // bool includeForecast (optional, default false)
        // int lastReadingId (optional, default 0)
        // bool returnUtc - (optional, default false) if true, all reading timestamps will be returned as UTC; otherwise region time will be used.
        [FunctionName("GetGageReadingsUTC")]
        public static async Task<IActionResult> GetGageReadingsUTC(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                int regionId = FzCommon.Constants.SvpaRegionId;
                string q = req.Query["regionId"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Int32.TryParse(q, out regionId))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid region id '{0}'", q));
                    }
                }
                string id = req.Query["id"];
                if (String.IsNullOrEmpty(id))
                {
                    return new BadRequestObjectResult("Missing required parameter 'id'");
                }
                DateTime? fromDateTime = null;
                q = req.Query["fromDateTime"];
                if (!String.IsNullOrEmpty(q))
                {
                    DateTime tmp;
                    if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid from date '{0}'", q));
                    }
                    fromDateTime = tmp;
                }
                DateTime? toDateTime = null;
                q = req.Query["toDateTime"];
                if (!String.IsNullOrEmpty(q))
                {
                    DateTime tmp;
                    if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid to date '{0}'", q));
                    }
                    toDateTime = tmp;
                }
                bool showDeletedReadings = false;
                q = req.Query["showDeletedReadings"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out showDeletedReadings))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid showDeletedReadings value '{0}'", q));
                    }
                }
                bool showMissingReadings = false;
                q = req.Query["showMissingReadings"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out showMissingReadings))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid showMissingReadings value '{0}'", q));
                    }
                }
                bool getMinimalReadings = false;
                q = req.Query["getMinimalReadings"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out getMinimalReadings))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid getMinimalReadings value '{0}'", q));
                    }
                }
                bool includeStatus = false;
                q = req.Query["includeStatus"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out includeStatus))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid includeStatus value '{0}'", q));
                    }
                }
                bool includePredictions = false;
                q = req.Query["includePredictions"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out includePredictions))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid includePredictions value '{0}'", q));
                    }
                }
                bool includeForecast = false;
                q = req.Query["includeForecast"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out includeForecast))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid includeForecast value '{0}'", q));
                    }
                }
                int lastReadingId = 0;
                q = req.Query["lastReadingId"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Int32.TryParse(q, out lastReadingId))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid lastReadingId value '{0}'", q));
                    }
                }
                bool returnUtc = false;
                q = req.Query["returnUtc"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out returnUtc))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid returnUtc value '{0}'", q));
                    }
                }

                ReadingStoreResponse resp
                        = await ReadingSvc.GetGageReadingsUTC(regionId,
                                                              id,
                                                              fromDateTime,
                                                              toDateTime,
                                                              showDeletedReadings,
                                                              showMissingReadings,
                                                              getMinimalReadings,
                                                              includeStatus,
                                                              includePredictions,
                                                              includeForecast,
                                                              lastReadingId,
                                                              returnUtc);

                // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
                // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
                // quoting the way OkObjectResult does.
                return new ContentResult() { Content = JsonConvert.SerializeObject(resp), ContentType = "application/json" };
            }
            catch (Exception e)
            {
                string error = String.Format("ReadingSvc.GetGageReadingsUTC: Query string: {0}", req.QueryString);
                ErrorManager.ReportException(ErrorSeverity.Major, error, e);
                throw;
            }
        }

        // Query parameters:
        // int regionId (optional, defaults to The One True Region)
        // string gageIds (comma-separated; use slashes to indicate a series of gages to be summed)
        // DateTime fromDateTime - beginning timestamp of historical data to include
        // DateTime toDateTime - ending timestamp of historical data to include (should be now, generally)
        // bool returnUtc - (optional, default false) if true, all reading timestamps will be returned as UTC; otherwise region time will be used.
        [FunctionName("GetForecastsUTC")]
        public static async Task<IActionResult> GetForecastsUTC(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                int regionId = FzCommon.Constants.SvpaRegionId;
                string q = req.Query["regionId"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Int32.TryParse(q, out regionId))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid region id '{0}'", q));
                    }
                }
                string gageIds = req.Query["gageIds"];
                if (String.IsNullOrEmpty(gageIds))
                {
                    return new BadRequestObjectResult("Missing required parameter 'gageIds'");
                }
                DateTime tmp;
                DateTime fromDateTime;
                q = req.Query["fromDateTime"];
                if (String.IsNullOrEmpty(q))
                {
                    return new BadRequestObjectResult("Missing required parameter 'fromDateTime'");
                }
                if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                {
                    return new BadRequestObjectResult(String.Format("Invalid from date '{0}'", q));
                }
                fromDateTime = tmp;
                DateTime toDateTime;
                q = req.Query["toDateTime"];
                if (String.IsNullOrEmpty(q))
                {
                    return new BadRequestObjectResult("Missing required parameter 'toDateTime'");
                }
                if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                {
                    return new BadRequestObjectResult(String.Format("Invalid to date '{0}'", q));
                }
                toDateTime = tmp;
                bool returnUtc = false;
                q = req.Query["returnUtc"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Boolean.TryParse(q, out returnUtc))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid returnUtc value '{0}'", q));
                    }
                }

                Dictionary<string, ReadingStoreResponse> result = await ReadingSvc.GetForecastsUTC(regionId, gageIds, fromDateTime, toDateTime, returnUtc);
                if (result == null)
                {
                    return new BadRequestObjectResult("An invalid parameter was encountered.");
                }

                // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
                // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
                // quoting the way OkObjectResult does.
                return new ContentResult() { Content = JsonConvert.SerializeObject(result), ContentType = "application/json" };
            }
            catch (Exception e)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReadingSvc.GetForecastsUTC", e);
                throw;
            }
        }
        
        //$ TODO: Auth for non-readonly methods

        //$ TODO: Should these be post?  There's no reason to go full REST for stuff like this...

        // Parameters:
        // int readingId
        // string locationId
        [FunctionName("MarkReadingAsDeleted")]
        public static async Task<IActionResult> MarkReadingAsDeleted(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                int readingId = 0;
                string locationId;
                string q = req.Query["readingId"];
                if (String.IsNullOrEmpty(q))
                {
                    return new BadRequestObjectResult("Missing required parameter 'readingId'");
                }
                if (!Int32.TryParse(q, out readingId))
                {
                    return new BadRequestObjectResult("Invalid parameter 'readingId'");
                }
                locationId = req.Query["locationId"];
                if (String.IsNullOrEmpty(locationId))
                {
                    return new BadRequestObjectResult("Missing required parameter 'locationId'");
                }
                if (await ReadingSvc.MarkReadingAsDeleted(locationId, readingId))
                {
                    return new OkResult();
                }
                return new BadRequestObjectResult("Invalid request.");
            }
            catch (Exception e)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReadingSvc.MarkReadingAsDeleted", e);
                throw;
            }
        }

        [FunctionName("MarkReadingAsUndeleted")]
        public static async Task<IActionResult> MarkReadingAsUndeleted(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                int readingId = 0;
                string locationId;
                string q = req.Query["readingId"];
                if (String.IsNullOrEmpty(q))
                {
                    return new BadRequestObjectResult("Missing required parameter 'readingId'");
                }
                if (!Int32.TryParse(q, out readingId))
                {
                    return new BadRequestObjectResult("Invalid parameter 'readingId'");
                }
                locationId = req.Query["locationId"];
                if (String.IsNullOrEmpty(locationId))
                {
                    return new BadRequestObjectResult("Missing required parameter 'locationId'");
                }
                if (await ReadingSvc.MarkReadingAsUndeleted(locationId, readingId))
                {
                    return new OkResult();
                }
                return new BadRequestObjectResult("Invalid request.");
            }
            catch (Exception e)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReadingSvc.MarkReadingAsUndeleted", e);
                throw;
            }
        }

        //
        // "New" API: Method for fetching front-page all-gages charts/status
        //

        // Query parameters:
        // int regionId (optional, defaults to The One True Region)
        // DateTime fromDateTime
        // DateTime toDateTime
        [FunctionName("GetGageStatusAndRecentReadings")]
        public static async Task<IActionResult> GetGageStatusAndRecentReadings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                DateTime tmp;

                int regionId = FzCommon.Constants.SvpaRegionId;
                string q = req.Query["regionId"];
                if (!String.IsNullOrEmpty(q))
                {
                    if (!Int32.TryParse(q, out regionId))
                    {
                        return new BadRequestObjectResult(String.Format("Invalid region id '{0}'", q));
                    }
                }
                q = req.Query["fromDateTime"];
                if (String.IsNullOrEmpty(q))
                {
                    return new BadRequestObjectResult("Missing required parameter 'fromDateTime'");
                }
                DateTime fromDateTime;
                if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                {
                    return new BadRequestObjectResult(String.Format("Invalid from date '{0}'", q));
                }
                fromDateTime = tmp;
                q = req.Query["toDateTime"];
                if (String.IsNullOrEmpty(q))
                {
                    return new BadRequestObjectResult("Missing required parameter 'toDateTime'");
                }
                DateTime toDateTime;
                if (!DateTime.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out tmp))
                {
                    return new BadRequestObjectResult(String.Format("Invalid to date '{0}'", q));
                }
                toDateTime = tmp;

                ApiGageStatusAndRecentReadingsResponse resp
                        = await ReadingSvc.GetGageStatusAndRecentReadings(regionId,
                                                                          fromDateTime,
                                                                          toDateTime);

                // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
                // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
                // quoting the way OkObjectResult does.
                return new ContentResult() { Content = JsonConvert.SerializeObject(resp), ContentType = "application/json" };
            }
            catch (Exception e)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReadingSvc.GetGageStatusAndRecentReadings", e);
                throw;
            }
        }
    }
}
