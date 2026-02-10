#define ALLOW_HEIGHT_ONLY_READINGS
// #define DISCARD_MISMATCHED_READINGS

using FzCommon.ExternalModels.UsgsJsonData;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml;

namespace FzCommon
{
    // For new WDFN-style USGS APIs
    public class WdfnReading
    {
        public DateTime Timestamp { get; set; }
        public double? Value { get; set; }
    }
    public enum WdfnReadingType
    {
        Height = 0,
        Discharge = 1,
    }

    public class UsgsSite
    {
        [Required]
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string NoaaSiteId { get; set; }
        public bool NotifyForecasts { get; set; }
        public bool NoDischarge { get; set; }

        //$ TODO: RegionId parameter
        public static async Task<List<UsgsSite>> GetUsgsSites(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT * FROM UsgsSites", sqlcn);
            try
            {
                List<UsgsSite> ret = new List<UsgsSite>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "UsgsSite.GetUsgsSites", ex);
            }
            return null;
        }

        public static async Task<UsgsSite> GetUsgsSite(SqlConnection sqlcn, int siteId)
        {
            SqlCommand cmd = new SqlCommand($"SELECT * FROM UsgsSites WHERE SiteId={siteId}", sqlcn);
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "UsgsSite.GetUsgsSites", ex);
            }
            return null;
        }

        private static UsgsSite InstantiateFromReader(SqlDataReader reader)
        {
            return new UsgsSite()
            {
                SiteId = SqlHelper.Read<int>(reader, "SiteId"),
                SiteName = SqlHelper.Read<string>(reader, "SiteName"),
                Latitude = SqlHelper.Read<double?>(reader, "Latitude"),
                Longitude = SqlHelper.Read<double?>(reader, "Longitude"),
                NoaaSiteId = SqlHelper.Read<string>(reader, "NoaaSiteId"),
                NotifyForecasts = SqlHelper.Read<bool>(reader, "NotifyForecasts"),
                NoDischarge = SqlHelper.Read<bool>(reader, "NoDischarge"),
            };
        }

        const string VARCODE_DISCHARGE = "00060";
        const string VARCODE_WATERHEIGHT = "00065";

        public class UsgsReadingData
        {
            public double Latitude;
            public double Longitude;
            public string SiteName;
            public List<SensorReading> Readings;
        }
        public async Task<UsgsReadingData> FetchUsgsReadings(string listenerInfo, DateTime startDateUtc, DeviceBase device, SensorLocationBase location)
        {
            string url = this.GetUrl(startDateUtc);
            JsonSerializer jsonSerializer = new JsonSerializer();
            try
            {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // NOTE: Changing this error to Minor for now, because it's too noisy when
                    // the service is having intermittent issues.  If the service is unavailable
                    // for 6 hours straight, we'll find out via the gage monitor.  And in any case
                    // there's not really anything we can do about this error for now...
                    ErrorManager.ReportError(ErrorSeverity.Minor, "UsgsSite.FetchUsgsReadings", String.Format("Error fetching USGS site {0} readings: {1}", this.SiteId, response.ReasonPhrase));
                    return null;
                }

                UsgsJsonResponse usgsResponse = null;
                try
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            using (JsonTextReader jr = new JsonTextReader(sr))
                            {
                                usgsResponse = jsonSerializer.Deserialize<UsgsJsonResponse>(jr);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorManager.ReportException(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", ex);
                    return null;
                }

                if (usgsResponse == null)
                {
                    ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", String.Format("Error fetching USGS site {0}: could not deserialize response", this.SiteId));
                    return null;
                }

                // Do some basic validation of the overall format.  This could obviously be much more robust...
                if (usgsResponse.Value.TimeSeries.Count < 1)
                {
                    ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", "USGS response didn't have any timeseries");
                    return null;
                }
                TimeSeries dischargeSeries = null;
                TimeSeries waterHeightSeries = null;
                foreach (TimeSeries timeSeries in usgsResponse.Value.TimeSeries)
                {
                    if (timeSeries.Variable.VariableCode.Count < 1)
                    {
                        ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", String.Format("Time series {0} had no variables", timeSeries.Name));
                        return null;
                    }
                    switch (timeSeries.Variable.VariableCode[0].Value)
                    {
                        case VARCODE_DISCHARGE:
                            dischargeSeries = timeSeries;
                            break;
                        case VARCODE_WATERHEIGHT:
                            waterHeightSeries = timeSeries;
                            break;
                        default:
                            ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", String.Format("Time series {0} had unexpected variable code {1}", timeSeries.Name, timeSeries.Variable.VariableCode[0].Value));
                            return null;
                    }
                    if (timeSeries.Values.Count < 1)
                    {
                        ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", String.Format("Time series {0} had no values", timeSeries.Name));
                        return null;
                    }
                }

                if (waterHeightSeries == null)
                {
                    ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", "USGS response didn't have a waterHeight TimeSeries");
                    return null;
                }
                if (dischargeSeries != null)
                {
                    if (dischargeSeries.Values[0].Value.Count != waterHeightSeries.Values[0].Value.Count)
                    {
#if ALLOW_HEIGHT_ONLY_READINGS
                        dischargeSeries = null;
#else
                        // This happens frequently when new readings come in. For whatever reason, the discharge
                        // readings are often delayed relative to the water height readings. As long as they aren't
                        // too far out of synch, we can just ignore this.
                        if (dischargeSeries.Values[0].Value.Count > 20 ||  waterHeightSeries.Values[0].Value.Count > 20)
                        {
#if DISCARD_MISMATCHED_READINGS
                            int i = 0;
                            while (i < dischargeSeries.Values[0].Value.Count && i < waterHeightSeries.Values[0].Value.Count)
                            {
                                if (dischargeSeries.Values[0].Value[i].DateTime.Ticks != waterHeightSeries.Values[0].Value[i].DateTime.Ticks)
                                {
                                    waterHeightSeries.Values[0].Value.RemoveAt(i);
                                }
                                else
                                {
                                    i++;
                                }
                            }
                            while (i < waterHeightSeries.Values[0].Value.Count)
                            {
                                waterHeightSeries.Values[0].Value.RemoveAt(i);
                            }
                            while (i < dischargeSeries.Values[0].Value.Count)
                            {
                                dischargeSeries.Values[0].Value.RemoveAt(i);
                            }
                        }
                        else
                        {
                            // We haven't gotten enough readings to worry about the mismatch yet.  Just return null, and
                            // we'll deal with this if we get more mismatches later.
                            return null;
                        }
#else
                            ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", String.Format("USGS response had mismatch between discharge count {0} and water height count {1}.\n URL: {2}", dischargeSeries.Values[0].Value.Count, waterHeightSeries.Values[0].Value.Count, url));
                        }
                        return null;
#endif
#endif
                    }
                }

                // Now we can extract the data.
                UsgsReadingData readingData = new UsgsReadingData();

                // Assume the metadata is the same for both time series...
                SourceInfo source = usgsResponse.Value.TimeSeries[0].SourceInfo;
                readingData.SiteName = source.SiteName;
                readingData.Latitude = source.GeoLocation.GeogLocation.Latitude;
                readingData.Longitude = source.GeoLocation.GeogLocation.Longitude;

                // Step through the series together; assume they're in the same order
                // and that they all have the same time values.  For now, it's an error
                // if they don't; this could be made more robust also...
                readingData.Readings = new List<SensorReading>();
                for (int i = 0; i < waterHeightSeries.Values[0].Value.Count; i++)
                {
                    TimeSeriesValue dischargeValue = (dischargeSeries == null) ? null : dischargeSeries.Values[0].Value[i];
                    TimeSeriesValue waterHeightValue = waterHeightSeries.Values[0].Value[i];

                    if (dischargeValue != null && dischargeValue.DateTime != waterHeightValue.DateTime)
                    {
                        ErrorManager.ReportError(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", String.Format("USGS response had reading datetime mismatch: {0} vs {1}", dischargeValue.DateTime, waterHeightValue.DateTime));
                        return null;
                    }

                    double waterHeightInches = waterHeightValue.Value * 12.0;
                    double groundHeightInches = 0;
                    if (location.GroundHeight.HasValue)
                    {
                        // The location has not been converted for display, so its internal values are in inches.
                        groundHeightInches = location.GroundHeight.Value;
                    }
                    double distanceReadingInches = groundHeightInches - waterHeightInches;
                    SensorReading reading = new SensorReading()
                    {
                        ListenerInfo = listenerInfo,
                        Timestamp = waterHeightValue.DateTime.ToUniversalTime(),
                        LocationId = location.Id,
                        DeviceId = device.DeviceId,
                        GroundHeight = groundHeightInches,
                        DistanceReading = distanceReadingInches,
                        WaterHeight = waterHeightInches,
                        WaterDischarge = dischargeValue == null ? (double?)null : dischargeValue.Value,
                    };

                    reading.AdjustReadingForLocation(location);
                    reading.AdjustReadingForDevice(device);

                    // USGS gages appear to return "-999999" for WaterDischarge when the gage readings
                    // are suspect (in particular, when the gage is "Ice affected").  If that happens we
                    // should save the reading as deleted.

                    // UPDATE: Apparently they can also sometimes return a height of "-999999".  So we'll
                    // filter if either number is <0.
                    if (reading.WaterDischarge < 0 || reading.WaterHeight < 0)
                    {
                        reading.IsDeleted = true;
                        reading.IsFiltered = true;
                        reading.DeleteReason = (reading.WaterDischarge < 0)
                                               ? "Filtered (invalid water discharge)"
                                               : "Filtered (invalid water height)";
                    }

                    readingData.Readings.Add(reading);
                }

                return readingData;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "UsgsSite.FetchUsgsReadings", ex);
            }
            return null;
        }

        private string GetUrl(DateTime startDateUtc)
        {
            string urlFormat = FzConfig.Config[FzConfig.Keys.UsgsWaterServiceUrlFormat];
            TimeSpan period = DateTime.UtcNow - startDateUtc;
            string periodString = XmlConvert.ToString(period);
            return String.Format(urlFormat, this.SiteId, periodString);
        }

        #region New WDFN Data Handling
        public const int DEFAULT_WDFN_READING_LIMIT = 10000;

        public async Task<List<WdfnReading>?> FetchRawWdfnReadings(DateTime startDateUtc,
                                                                   DateTime endDateUtc,
                                                                   DeviceBase device,
                                                                   WdfnReadingType readingType,
                                                                   int readingLimit = DEFAULT_WDFN_READING_LIMIT)
        {
            string parameterCode = VARCODE_WATERHEIGHT;
            if (readingType == WdfnReadingType.Discharge)
            {
                parameterCode = VARCODE_DISCHARGE;
            }
            string url = this.GetWdfnUrl(startDateUtc, endDateUtc, parameterCode, readingLimit);
            JsonSerializer jsonSerializer = new JsonSerializer();
            HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, url);
            WdfnJsonResponse wdfnResponse = null;
            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        using (JsonTextReader jr = new JsonTextReader(sr))
                        {
                            wdfnResponse = jsonSerializer.Deserialize<WdfnJsonResponse>(jr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorManager.ReportUrlException(ErrorSeverity.Major, "UsgsSite.FetchRawWdfnReadings", url, e);
                return null;
            }
            List<WdfnReading> ret = new();
            if (wdfnResponse != null && wdfnResponse.Features != null)
            {
                foreach (WdfnDataFeature f in wdfnResponse.Features)
                {
                    if (f.Properties.Parameter_Code != parameterCode)
                    {
                        string error = String.Format("USGS WDFN Error: Requested parameter code {0}, received {1}.  URL: {2}", parameterCode, f.Properties.Parameter_Code, url);
                        ErrorManager.ReportError(ErrorSeverity.Major, "Usgs", error);
                        return null;
                    }
                    ret.Add(new WdfnReading()
                    {
                        Timestamp = f.Properties.Time.ToUniversalTime(),
                        Value = f.Properties.Value,
                    });
                }
            }
            return ret;
        }

        private string GetWdfnUrl(DateTime startDateUtc, DateTime endDateUtc, string parameterCode, int readingLimit)
        {
            string apiKey = FzConfig.Config[FzConfig.Keys.UsgsWDFNApiKey];
            string fullSiteId = "USGS-" + this.SiteId.ToString();
            string urlFormat = FzConfig.Config[FzConfig.Keys.UsgsWDFNUrlFormat];
            string periodString = String.Format("{0}Z/{1}Z", DateTimeToRfc3339String(startDateUtc), DateTimeToRfc3339String(endDateUtc));
            return String.Format(urlFormat, readingLimit, fullSiteId, parameterCode, periodString, apiKey);
        }
        #endregion

        private static string DateTimeToRfc3339String(DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                throw new ApplicationException(String.Format("Expected UTC DateTime in DateTimeToRfc3339String; got {0}", dt.Kind));
            }
            return dt.ToString("yyyy-MM-dd'T'HH:mm:ss", DateTimeFormatInfo.InvariantInfo);
        }
    }
}


