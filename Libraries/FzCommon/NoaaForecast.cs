using System.Data;
using System.Xml.Serialization;
using Microsoft.Data.SqlClient;
using FzCommon.ExternalModels.NwrfcForecast;

namespace FzCommon
{
    public class NoaaForecastSet
    {
        public List<NoaaForecast> Forecasts     { get; set; }
        public DateTime Created                 { get; set; }

        public NoaaForecastSet()
        {
            this.Created = DateTime.MinValue;
            this.Forecasts = new List<NoaaForecast>();
        }

        public NoaaForecast GetForecast(string noaaSiteId)
        {
            return this.Forecasts.FirstOrDefault(f => f.NoaaSiteId == noaaSiteId);
        }

        public static async Task<NoaaForecastSet> GetLatestForecastSet(SqlConnection sqlcn)
        {
            return await GetForecastSet(sqlcn, "GetLatestNoaaForecastSet");
        }

        public static async Task<NoaaForecastSet> GetPreviousForecastSet(SqlConnection sqlcn)
        {
            return await GetForecastSet(sqlcn, "GetPreviousNoaaForecastSet");
        }

        // This is for testing; it allows us to load a known set of forecasts.
        public static async Task<NoaaForecastSet> GetForecastSetForForecastId(SqlConnection sqlcn, int forecastId)
        {
            return await GetForecastSet(sqlcn, "GetNoaaForecastSetForForecastId", forecastId);
        }

        private static async Task<NoaaForecastSet> GetForecastSet(SqlConnection sqlcn, string sprocName, int? forecastId = null)
        {
            NoaaForecastSet forecastSet = new NoaaForecastSet();
            List<string> ids = new List<string>();
            using (SqlCommand cmd = new SqlCommand(sprocName, sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (forecastId.HasValue)
                {
                    cmd.Parameters.Add("@ForecastId", SqlDbType.Int).Value = forecastId.Value;
                }
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        NoaaForecast forecast = NoaaForecast.InstantiateFromReader(dr);
                        forecast.Data = new List<NoaaForecastItem>();
                        forecastSet.Forecasts.Add(forecast);
                        ids.Add(forecast.ForecastId.ToString());
                    }
                }
            }
            forecastSet.Created = forecastSet.Forecasts[0].Created;

            using (SqlCommand cmd = new SqlCommand("GetNoaaForecastReadingsForSet", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@IdList", SqlDbType.VarChar, 200).Value = String.Join(',', ids);

                int i = 0;
                NoaaForecast cur = forecastSet.Forecasts[i];
                
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        NoaaForecastItem item = NoaaForecastItem.InstantiateFromReader(dr);
                        if (item.ForecastId > cur.ForecastId)
                        {
                            while (i < forecastSet.Forecasts.Count && item.ForecastId > cur.ForecastId)
                            {
                                i++;
                                cur = forecastSet.Forecasts[i];
                            }
                            if (i >= forecastSet.Forecasts.Count)
                            {
                                break;
                            }
                        }
                        cur.Data.Add(item);
                    }
                }
            }
            return forecastSet;
        }
    }

    public class NoaaForecastItem
    {
        public int ForecastId       { get; set; }
        public DateTime Timestamp   { get; set; }
        public double? Stage        { get; set; }
        public double? Discharge    { get; set; }

        public static NoaaForecastItem InstantiateFromReader(SqlDataReader dr)
        {
            return new NoaaForecastItem()
            {
                ForecastId = SqlHelper.Read<int>(dr, "ForecastId"),
                Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
                Stage = SqlHelper.Read<double?>(dr, "Stage"),
                Discharge = SqlHelper.Read<double?>(dr, "Discharge"),
            };
        }
    }

    public class NoaaForecast
    {
        public string NoaaSiteId            { get; set; }
        public DateTime Created             { get; set; }
        public int ForecastId               { get; set; }
        public string Description           { get; set; }
        public string County                { get; set; }
        public string State                 { get; set; }
        public double? Latitude             { get; set; }
        public double? Longitude            { get; set; }
        public double? Elevation            { get; set; }
        public double? BankFullStage        { get; set; }
        public double? FloodStage           { get; set; }
        public double? CurrentWaterHeight   { get; set; }
        public double? CurrentDischarge     { get; set; }
        public List<NoaaForecastItem> Data  { get; set; }

        public NoaaForecastItem[] Peaks
        {
            get
            {
                return this.GetPeaks().ToArray();
            }
        }

        public static async Task<NoaaForecast> FetchNewForecast(string noaaSiteId)
        {
            try
            {
                string url = GetUrl(noaaSiteId, DataTypeStage);
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorManager.ReportError(ErrorSeverity.Major, "NoaaForecast.FetchNewForecast", String.Format("Error fetching NOAA forecast: {0}", response.ReasonPhrase));
                    return null;
                }

                XmlSerializer ser = new XmlSerializer(typeof(HydroMetData));
                HydroMetData hmd = (HydroMetData)ser.Deserialize(await response.Content.ReadAsStreamAsync());

                if (hmd.Site.SiteId != noaaSiteId)
                {
                    ErrorManager.ReportError(ErrorSeverity.Major, "NoaaForecast.FetchNewForecast", String.Format("Error fetching NOAA forecast: site ID mismatch ({0} != {1})", hmd.Site.SiteId, noaaSiteId));
                    return null;
                }

                // For now this is basically ok -- it just means that this site has no forecast, so we can skip it.
                if (hmd.Site.ForecastData == null || hmd.Site.ForecastData.Length < 1)
                {
                    //$ TODO: Do we want to even report this?  For now it's normal...
                    // ErrorManager.ReportError(ErrorSeverity.Minor, "NoaaForecast.FetchNewForecast", "Error fetching NOAA forecast: no returned forecast data");
                    return null;
                }

                NoaaForecast forecast = new NoaaForecast()
                {
                    NoaaSiteId = hmd.Site.SiteId,
                    Created = hmd.Site.ForecastData[0].CreationDateTime,
                    Description = hmd.Site.Description,
                    County = hmd.Site.County,
                    State = hmd.Site.State,
                    Latitude = hmd.Site.Latitude,
                    Longitude = hmd.Site.Longitude,
                    Elevation = hmd.Site.Elevation,
                    BankFullStage = hmd.Site.BankFullStage,
                    FloodStage = hmd.Site.FloodStage,
                };
                forecast.Data = new List<NoaaForecastItem>();

                //$ TODO: look at/validate other fields?

                foreach (HydroMetForecastValue val in hmd.Site.ForecastData)
                {
                    if (val.CreationDateTime != forecast.Created)
                    {
                        ErrorManager.ReportError(ErrorSeverity.Minor, "NoaaForecast.FetchNewForecast", "creationDateTime mismatch in forecast data");
                        // just keep going.
                    }

                    //$ TODO: look at/validate other fields?
                    NoaaForecastItem fi = new NoaaForecastItem()
                    {
                        Timestamp = val.DataDateTime,
                        Stage = val.Stage,
                        Discharge = val.Discharge,
                    };
                    forecast.Data.Add(fi);
                }

                return forecast;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "NoaaForecast.FetchForecast", ex);
            }
            return null;
        }

        public static async Task<NoaaForecast?> GetLatestSavedForecast(SqlConnection sqlcn, string noaaSiteId)
        {
            NoaaForecast forecast = null;
            using (SqlCommand cmd = new SqlCommand("GetLatestNoaaForecastForSite", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@NoaaSiteId", SqlDbType.VarChar, 100).Value = noaaSiteId;
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    if (!await dr.ReadAsync())
                    {
                        return null;
                    }
                    forecast = NoaaForecast.InstantiateFromReader(dr);
                    forecast.Data = new List<NoaaForecastItem>();
                    if (await dr.NextResultAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            forecast.Data.Add(NoaaForecastItem.InstantiateFromReader(dr));
                        } 
                    }
                }
            }

            return forecast;
        }

        public bool ExceedsDischargeThreshold(double discharge)
        {
            foreach (NoaaForecastItem item in this.Data)
            {
                if (item.Discharge > discharge)
                {
                    return true;
                }
            }
            return false;
        }

        //$ TODO: Do we really just want to use the forecast creation date?  Or should
        //$ we check all the predictions to see if they've changed?
        public bool ForecastHasChanged(NoaaForecast previous)
        {
            if (previous.Created != this.Created)
            {
                return true;
            }

            return false;
        }

        private List<NoaaForecastItem> m_peaks = null;
        public List<NoaaForecastItem> GetPeaks()
        {
            if (this.m_peaks != null)
            {
                return this.m_peaks;
            }
            this.m_peaks = new List<NoaaForecastItem>();

            // Check for a first-reading peak
            if (this.CurrentDischarge.HasValue && this.Data.Count > 1)
            {
                if (this.Data[0].Discharge > this.CurrentDischarge && this.Data[0].Discharge > this.Data[1].Discharge)
                {
                    this.m_peaks.Add(this.Data[0]);
                }
            }
            for (int i = 1; i < this.Data.Count - 1; i++)
            {
                if (this.Data[i].Discharge > this.Data[i - 1].Discharge && this.Data[i].Discharge > this.Data[i + 1].Discharge)
                {
                    this.m_peaks.Add(this.Data[i]);
                }
            }

            return this.m_peaks;
        }

        public static NoaaForecast SumForecasts(List<NoaaForecast> sources)
        {
            NoaaForecast sum = new NoaaForecast()
            {
                NoaaSiteId = "",
                Created = sources[0].Created,
                Data = new List<NoaaForecastItem>(),
                BankFullStage = 0,
                FloodStage = 0,
            };
            List<Queue<NoaaForecastItem>> subForecasts = new List<Queue<NoaaForecastItem>>();
            foreach (NoaaForecast source in sources)
            {
                //$ TODO: What should this do if the forecasts have different Created dates?
                sum.BankFullStage += source.BankFullStage;
                sum.FloodStage += source.FloodStage;
                subForecasts.Add(new Queue<NoaaForecastItem>(source.Data));
            }

            NoaaForecastItem subItem;
            while (subForecasts[0].TryDequeue(out subItem))
            {
                NoaaForecastItem sumForecast = new NoaaForecastItem()
                {
                    Timestamp = subItem.Timestamp,
                    Discharge = subItem.Discharge,
                };

                bool skip = false;
                for (int i = 1; i < subForecasts.Count; i++)
                {
                    NoaaForecastItem candidate;
                    if (!subForecasts[i].TryPeek(out candidate))
                    {
                        skip = true;
                        break;
                    }

                    //$ TODO: Should there be an epsilon so timestamps within a minute are accepted?
                    while (candidate.Timestamp < subItem.Timestamp)
                    {
                        subForecasts[i].Dequeue();
                        if (!subForecasts[i].TryPeek(out candidate))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (!skip)
                    {
                        if (candidate.Timestamp > subItem.Timestamp)
                        {
                            skip = true;
                            break;
                        }
                        sumForecast.Discharge += candidate.Discharge;
                        subForecasts[i].Dequeue();
                    }
                }
                if (!skip)
                {
                    sum.Data.Add(sumForecast);
                }
            }
            return sum;
        }

        public async Task Save(SqlConnection sqlcn)
        {
            using (SqlCommand cmd = new SqlCommand("SaveNoaaForecast", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ForecastId", SqlDbType.Int).Value = this.ForecastId;
                cmd.Parameters.Add("@NoaaSiteId", SqlDbType.VarChar, 100).Value = this.NoaaSiteId;
                cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value = this.Created;
                if (this.Description != null) cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 200).Value = this.Description;
                if (this.County != null) cmd.Parameters.Add("@County", SqlDbType.NVarChar, 100).Value = this.County;
                if (this.State != null) cmd.Parameters.Add("@State", SqlDbType.NVarChar, 100).Value = this.State;
                if (this.Latitude.HasValue) cmd.Parameters.Add("@Latitude", SqlDbType.Float).Value = this.Latitude;
                if (this.Longitude.HasValue) cmd.Parameters.Add("@Longitude", SqlDbType.Float).Value = this.Longitude;
                if (this.Elevation.HasValue) cmd.Parameters.Add("@Elevation", SqlDbType.Float).Value = this.Elevation;
                if (this.BankFullStage.HasValue) cmd.Parameters.Add("@BankFullStage", SqlDbType.Float).Value = this.BankFullStage;
                if (this.FloodStage.HasValue) cmd.Parameters.Add("@FloodStage", SqlDbType.Float).Value = this.FloodStage;
                if (this.CurrentWaterHeight.HasValue) cmd.Parameters.Add("@CurrentWaterHeight", SqlDbType.Float).Value = this.CurrentWaterHeight;
                if (this.CurrentDischarge.HasValue) cmd.Parameters.Add("@CurrentDischarge", SqlDbType.Float).Value = this.CurrentDischarge;
                cmd.Parameters.Add("@PurgeData", SqlDbType.Bit).Value = 1;
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        this.ForecastId = SqlHelper.Read<int>(dr, "ForecastId");
                    }
                }
            }

            using (SqlCommand cmd = new SqlCommand("SaveNoaaForecastDataEntry", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (NoaaForecastItem item in this.Data)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add("@ForecastId", SqlDbType.Int).Value = this.ForecastId;
                    cmd.Parameters.Add("@Timestamp", SqlDbType.DateTime).Value = item.Timestamp;
                    cmd.Parameters.Add("@Stage", SqlDbType.Float).Value = item.Stage;
                    cmd.Parameters.Add("@Discharge", SqlDbType.Float).Value = item.Discharge;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static NoaaForecast InstantiateFromReader(SqlDataReader dr)
        {
            return new NoaaForecast()
            {
                NoaaSiteId = SqlHelper.Read<string>(dr, "NoaaSiteId"),
                Created = SqlHelper.Read<DateTime>(dr, "Created"),
                ForecastId = SqlHelper.Read<int>(dr, "ForecastId"),
                Description = SqlHelper.Read<string>(dr, "Description"),
                County = SqlHelper.Read<string>(dr, "County"),
                State = SqlHelper.Read<string>(dr, "State"),
                Latitude = SqlHelper.Read<double?>(dr, "Latitude"),
                Longitude = SqlHelper.Read<double?>(dr, "Longitude"),
                Elevation = SqlHelper.Read<double?>(dr, "Elevation"),
                BankFullStage = SqlHelper.Read<double?>(dr, "BankFullStage"),
                FloodStage = SqlHelper.Read<double?>(dr, "FloodStage"),
                CurrentWaterHeight = SqlHelper.Read<double?>(dr, "CurrentWaterHeight"),
                CurrentDischarge = SqlHelper.Read<double?>(dr, "CurrentDischarge"),
            };
        }

        //$ TODO: move to config, probably
        private const string UrlFormat = "https://www.nwrfc.noaa.gov/xml/xml.cgi?id={0}&pe={1}&dtype=b&numdays=0";

        //$ TODO: remove this if HG always includes discharge
        private const string DataTypeStage = "HG";
        private const string DataTypeDischarge = "QR";

        private static string GetUrl(string noaaSiteId, string dataType)
        {
            return String.Format(UrlFormat, noaaSiteId, dataType);
        }
    }
}

