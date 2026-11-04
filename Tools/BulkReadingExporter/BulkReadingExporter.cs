using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using FzCommon;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace BulkReadingExporter;

public class DumpableReading
{
    public DateTime UtcTimestamp { get; set; }
    public double WaterHeightFeet { get; set; }
    public double DischargeCfs { get; set; }
}

public class JsonDump
{
    public int LocationId { get; set; }
    public required string LocationName { get; set; }
    public double LocationLatitude { get; set; }
    public double LocationLongitude { get; set; }
    public required List<DumpableReading> Readings { get; set; }
}

[CultureInfo("InvariantCulture")]
public class CsvDumpableReading : DumpableReading
{
    public CsvDumpableReading(DumpableReading baseReading)
    {
        this.UtcTimestamp = baseReading.UtcTimestamp;
        this.WaterHeightFeet = baseReading.WaterHeightFeet;
        this.DischargeCfs = baseReading.DischargeCfs;
    }
    public int LocationId { get; set; }
    public required string LocationName { get; set; }
    public double LocationLatitude { get; set; }
    public double LocationLongitude { get; set; }
}

public class BulkReadingExporter
{
    public bool VERBOSE = false;

    // For now I'm pinning this at July 2026, until I figure out how to do periodic updates of this data set....
    public DateTime TO_DATE = new(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);

    public readonly CsvConfiguration configCsv;
    public readonly CsvConfiguration configTsv;

    public BulkReadingExporter()
    {
        this.configCsv = CsvConfiguration.FromAttributes<CsvDumpableReading>();
        this.configTsv = CsvConfiguration.FromAttributes<CsvDumpableReading>();
        this.configTsv.Delimiter = "\t";
    }

    public async Task Run(SqlConnection sqlcn)
    {
        List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
        foreach (SensorLocationBase loc in locations)
        {
            if (loc.IsDeleted)
            {
                if (VERBOSE)
                {
                    Console.Write("Location {0}: {1}...Skipping (deleted)", loc.Id, loc.LocationName);
                }
                continue;
            }
            if (!loc.IsPublic)
            {
                if (VERBOSE)
                {
                    Console.Write("Location {0}: {1}...Skipping (private)", loc.Id, loc.LocationName);
                }
                continue;
            }
            await DumpLocationReadings(sqlcn, loc);
        }
    }

    private async Task DumpLocationReadings(SqlConnection sqlcn, SensorLocationBase loc)
    {
        int readingCount = 0;

        string filenameBase = String.Format("readings-location-{0}-through-{1}", loc.Id, TO_DATE.ToString("yyyy-MM-dd"));

        Console.Write("Location {0} ({1})...fetching...", loc.Id, loc.LocationName);
        List<SensorReading> allReadings = await SensorReading.GetMinimalReadingsForLocation(sqlcn, loc.Id, null, null, TO_DATE);

        using FileStream fsJson = new(filenameBase + ".json", FileMode.Create, FileAccess.ReadWrite);
        using StreamWriter swJson = new(fsJson);
        using FileStream fsCsv = new(filenameBase + ".csv", FileMode.Create, FileAccess.ReadWrite);
        using StreamWriter swCsv = new(fsCsv);
        using FileStream fsTsv = new(filenameBase + ".tsv", FileMode.Create, FileAccess.ReadWrite);
        using StreamWriter swTsv = new(fsTsv);

        using CsvWriter csv = new CsvWriter(swCsv, configCsv);
        using CsvWriter tsv = new CsvWriter(swTsv, configTsv);
        csv.WriteHeader<CsvDumpableReading>();
        csv.NextRecord();
        tsv.WriteHeader<CsvDumpableReading>();
        tsv.NextRecord();
        JsonDump jsonDump = new()
        {
            LocationId = loc.Id,
            LocationName = loc.LocationName,
            LocationLatitude = loc.Latitude ?? 0,
            LocationLongitude = loc.Longitude ?? 0,
            Readings = [],
        };

        foreach (SensorReading sr in allReadings)
        {
            readingCount++;
            DumpableReading dr = new()
            {
                UtcTimestamp = sr.Timestamp,
                WaterHeightFeet = sr.WaterHeightFeet ?? 0,
                DischargeCfs = sr.WaterDischarge ?? 0,
            };
            jsonDump.Readings.Add(dr);
            CsvDumpableReading cdr = new(dr)
            {
                LocationId = loc.Id,
                LocationName = loc.LocationName,
                LocationLatitude = loc.Latitude ?? 0,
                LocationLongitude = loc.Longitude ?? 0,
            };
            csv.WriteRecord(cdr);
            csv.NextRecord();
            tsv.WriteRecord(cdr);
            tsv.NextRecord();
        }

        swJson.Write(JsonConvert.SerializeObject(jsonDump));

        Console.WriteLine("Dumped {0} readings", readingCount);
    }
}
