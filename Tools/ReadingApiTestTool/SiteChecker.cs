using System.Globalization;
using FzCommon;
using Newtonsoft.Json;

//$ TODO: remove the duplication here...
public class GageReading
{
    public int Id;
    public DateTime Timestamp;
    public double? WaterHeight;
    public double? GroundHeight;
    public double? WaterDischarge;
    public int? BatteryMillivolt;
    public double? BatteryPercent;
    public double? RoadSaddleHeight;
    public double? RSSI;
    public double? SNR;
    public bool IsDeleted;

    // This is true if this is a 'fake' reading to indicate that we
    // didn't receive an expected update
    public bool IsMissing;
}

public class ReadingStoreResponse
{
    public List<GageReading> Readings;
    public int LastReadingId;
    public bool NoData;
    public ApiLocationStatus Status;
    public ApiLocationStatus PeakStatus;
    public List<ApiGageReading> Predictions;
    public NoaaForecast NoaaForecast;
    public double PredictedFeetPerHour;
    public double PredictedCfsPerHour;

    // Will only be set if forecasts are requested.
    public double? DischargeStageOne;
    public double? DischargeStageTwo;

    // Temporary, for debugging, to compare predictions to reality.
    public List<ApiGageReading> ActualReadings;
}

public class SiteToCheck
{
    public SiteToCheck(UsgsSite site, DeviceBase device, SensorLocationBase location)
    {
        this.site = site;
        this.device = device;
        this.location = location;
    }

    readonly UsgsSite site;
    readonly DeviceBase device;
    readonly SensorLocationBase location;
    int lastReadingId = 0;

    static readonly JsonSerializer jsonSerializer = new();
    const bool VERBOSE = false;

    const string URL_BASE = "https://{0}/api/GetGageReadingsUTC?regionId=1&id={1}&fromDateTime={2}&toDateTime={3}&lastReadingId={4}&includeStatus=true&includePredictions=true";

    public async Task CheckSite()
    {
        DateTime now = DateTime.UtcNow;
        DateTime from = now.AddHours(-4);
        string url = String.Format(URL_BASE, FzConfig.Config["ReadingSvcHost"], this.location.PublicLocationId, from.ToString("o", CultureInfo.InvariantCulture), now.ToString("o", CultureInfo.InvariantCulture), this.lastReadingId);
        using HttpClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, url);
        HttpResponseMessage response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine();
            Console.WriteLine("Error checking for new readings!");
            return;
        }
        ReadingStoreResponse? readings = null;
        try
        {
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using StreamReader sr = new(stream);
            using JsonTextReader jr = new JsonTextReader(sr);
            readings = jsonSerializer.Deserialize<ReadingStoreResponse>(jr);
            if (readings == null)
            {
                throw new ApplicationException("Couldn't deserialize");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine("Error checking for new readings: {0}", e);
            return;
        }
        if (VERBOSE)
        {
            Console.WriteLine("checking {0} -- {1} readings", url, readings.Readings.Count);
        }

        if (readings.Status.FloodLevel == ApiFloodLevel.Flooding)
        {
            Console.WriteLine("FOUND FLOODING ON {0}", this.location.LocationName);
            Console.WriteLine("---------------------");
            List<SensorReading> allreadings = await SensorReading.GetAllReadingsForLocation(this.location.Id, null, from, now);
            foreach (SensorReading sr in allreadings)
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", sr.Id, sr.Timestamp, sr.WaterDischarge, sr.WaterHeight);
            }
            Console.WriteLine("---------------------");
        }

        if (readings.Readings.Count > 0)
        {
            this.lastReadingId = readings.Readings[0].Id;
        }
    }
};

