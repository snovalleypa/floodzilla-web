using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using FzCommon;

// Public API for ReadingSvc.

namespace ReadingSvc
{
    public class ReadingSvc
    {

        public static void Initialize()
        {
            FzConfig.Initialize();

            //$ TODO: Maybe move this to common code somewhere if it's going
            //$ to be used everywhere...

            //$ TODO: Consider adding DefaultValueHandling = DefaultValueHandling.Ignore
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() { AllowIntegerValues = false }},
            };

            //$ TODO: Any other configuration/initialization?

        }
        
        public static async Task<ReadingStoreResponse> GetGageReadings(int regionId,
                                                                       string id,
                                                                       DateTime? fromDateTime = null,
                                                                       DateTime? toDateTime = null,
                                                                       bool showDeletedReadings = false,
                                                                       bool showMissingReadings = false,
                                                                       bool getMinimalReadings = false,
                                                                       bool includeStatus = false,
                                                                       bool includePredictions = false,
                                                                       bool includeForecast = false, 
                                                                       int lastReadingId = 0,
                                                                       bool returnUtc = false)
        {
            return await ReadingStore.Store.GetGageReadings(regionId,
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
        }

        public static async Task<ReadingStoreResponse> GetGageReadingsUTC(int regionId,
                                                                          string id,
                                                                          DateTime? fromDateTime = null,
                                                                          DateTime? toDateTime = null,
                                                                          bool showDeletedReadings = false,
                                                                          bool showMissingReadings = false,
                                                                          bool getMinimalReadings = false,
                                                                          bool includeStatus = false,
                                                                          bool includePredictions = false,
                                                                          bool includeForecast = false, 
                                                                          int lastReadingId = 0,
                                                                          bool returnUtc = false)
{
            return await ReadingStore.Store.GetGageReadingsUTC(regionId,
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
        }

        public static async Task<bool> MarkReadingAsDeleted(string locationPublicId, int readingId)
        {
            return await ReadingStore.Store.MarkReadingAsDeleted(locationPublicId, readingId);
        }
        
        public static async Task<bool> MarkReadingAsUndeleted(string locationPublicId, int readingId)
        {
            return await ReadingStore.Store.MarkReadingAsUndeleted(locationPublicId, readingId);
        }

        //
        // "New" API: Method for fetching front-page all-gages charts/status
        //

        //$ TODO: Caching, move into ReadingStore, whatever ends up being best...
        public static async Task<ApiGageStatusAndRecentReadingsResponse>
            GetGageStatusAndRecentReadings(int regionId,
                                           DateTime fromDateTime,
                                           DateTime toDateTime)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                //$ TODO: Caching if necessary
                List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsForRegionAsync(sqlcn, regionId);

                Dictionary<int, List<SensorReading>> allReadings = await SensorReading.GetAllSensorReadingsInTimespan(sqlcn, fromDateTime, toDateTime);

                ApiGageStatusAndRecentReadingsResponse resp = new ApiGageStatusAndRecentReadingsResponse()
                {
                    Gages = new List<ApiGageStatusAndRecentReadings>(),
                };
                foreach (SensorLocationBase location in locations)
                {
                    if (!allReadings.ContainsKey(location.Id))
                    {
                        continue;
                    }
                    List<SensorReading> readings = allReadings[location.Id];

                    //$ TODO: If these are cached, do this in a local copy
                    location.ConvertValuesForDisplay();

                    Trends trends = TrendCalculator.CalculateWaterTrends(readings);
                    ApiGageStatusAndRecentReadings gs = new ApiGageStatusAndRecentReadings()
                    {
                        LocationId = location.PublicLocationId,
                        Readings = null,
                        Status = new ApiLocationStatus(location, trends, readings),
                    };

                    if (readings != null)
                    {
                        gs.Readings = new List<ApiGageReading>();
                        foreach (SensorReading sr in readings)
                        {
                            gs.Readings.Add(new ApiGageReading(sr, location));
                        }
                    }

                    resp.Gages.Add(gs);
                }
                return resp;
            }
        }

        public static async Task<Dictionary<string, ReadingStoreResponse>>
                GetForecastsUTC(int regionId,
                                string gageIds,
                                DateTime fromDateTime,
                                DateTime toDateTime,
                                bool returnUtc = false)
        {
            Dictionary<string, ReadingStoreResponse> result = new Dictionary<string, ReadingStoreResponse>();
            foreach (string gageId in gageIds.Split(','))
            {
                if (gageId.Contains("/"))
                {
                    string[] subGageIds = gageId.Split("/");

                    // Cheat and just recurse here...
                    Dictionary<string, ReadingStoreResponse> subResult = await GetForecastsUTC(regionId, String.Join(',', subGageIds), fromDateTime, toDateTime, returnUtc);

                    ReadingStoreResponse sums = new ReadingStoreResponse()
                    {
                        Readings = new List<GageReading>(),
                        LastReadingId = 0,
                        NoData = false,
                        Status = null,
                        PeakStatus = null,
                        Predictions = null,
                        NoaaForecast = new NoaaForecast(),
                        PredictedFeetPerHour = 0,
                        PredictedCfsPerHour = 0,
                        ActualReadings = null,
                    };

                    // Find out if we have a metagage that matches this exact set of gages; if so, return its flood-stage values.
                    Metagage? metagage = Metagages.FindMatchingMetagage(subGageIds);
                    if (metagage != null)
                    {
                        sums.DischargeStageOne = metagage.StageOne;
                        sums.DischargeStageTwo = metagage.StageTwo;
                    }

                    List<NoaaForecast> sourceForecasts = new List<NoaaForecast>();
                    List<Queue<GageReading>> subReadings = new List<Queue<GageReading>>();

                    // If we don't have one of the subresponses, bail
                    foreach (string subGageId in subGageIds)
                    {
                        // This basically means an invalid ID was passed in
                        if (!subResult.ContainsKey(subGageId))
                        {
                            return null;
                        }
                        ReadingStoreResponse subResp = subResult[subGageId];
                        if ((subResp == null) || (subResp.Readings == null) || (subResp.Readings.Count == 0))
                        {
                            return null;
                        }
                        subReadings.Add(new Queue<GageReading>(subResp.Readings));
                        if (subResp.NoaaForecast == null || subResp.NoaaForecast.Data == null)
                        {
                            return null;
                        }
                        sourceForecasts.Add(subResp.NoaaForecast);

                        sums.PredictedFeetPerHour += subResp.PredictedFeetPerHour;
                        sums.PredictedCfsPerHour += subResp.PredictedCfsPerHour;
                    }
                    sums.NoaaForecast = NoaaForecast.SumForecasts(sourceForecasts);

                    // First, add up the existing readings; just do WaterDischarge, because all the other
                    // values are meaningless when summed.
                    GageReading subReading;
                    while (subReadings[0].TryDequeue(out subReading))
                    {
                        GageReading sumReading = new GageReading()
                        {
                            Timestamp = subReading.Timestamp,
                            WaterDischarge = subReading.WaterDischarge,
                        };
                        bool skip = false;
                        for (int i = 1; i < subReadings.Count; i++)
                        {
                            GageReading candidate;
                            if (!subReadings[i].TryPeek(out candidate))
                            {
                                skip = true;
                                break;
                            }

                            //$ TODO: Should there be an epsilon so timestamps within a minute are accepted?
                            while (candidate.Timestamp > subReading.Timestamp)
                            {
                                subReadings[i].Dequeue();
                                if (!subReadings[i].TryPeek(out candidate))
                                {
                                    skip = true;
                                    break;
                                }
                            }
                            if (!skip)
                            {
                                if (candidate.Timestamp < subReading.Timestamp)
                                {
                                    skip = true;
                                    break;
                                }
                                sumReading.WaterDischarge += candidate.WaterDischarge;
                                subReadings[i].Dequeue();
                            }
                        }
                        if (!skip)
                        {
                            sums.Readings.Add(sumReading);
                        }
                    }
                    result[gageId] = sums;
                }
                else
                {
                    result[gageId] = await GetGageReadingsUTC(regionId,
                                                              gageId,
                                                              fromDateTime,
                                                              toDateTime,
                                                              false, // showDeletedReadings
                                                              false, // showMissingReadings
                                                              true, // getMinimalReadings,
                                                              false, // includeStatus
                                                              true, // includePredictions,
                                                              true, // includeForecast,
                                                              0,    // lastReadingId
                                                              returnUtc);
                }
            }
            return result;
        }
    }
}
