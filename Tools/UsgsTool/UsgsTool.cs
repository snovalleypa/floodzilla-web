using Microsoft.Data.SqlClient;
using System.CommandLine;
using System.Data;
using System.Text;
using System.Xml;

using FzCommon;
using FzCommon.Fetchers;
using UsgsTool;

async static Task ExecList(List<UsgsSite> sites, int regionId)
{
    sites.ForEach((s) =>
    {
        Console.WriteLine("{0} (noaa {1}): {2}",
                          s.SiteId,
                          String.IsNullOrEmpty(s.NoaaSiteId) ? "<none>" : s.NoaaSiteId,
                          s.SiteName);
    });
}

//$ TODO: Limit this to an explicit date range?
async static Task ExecCompareGaugeHistory(SqlConnection sqlcn,
                                          DeviceBase? deviceTest,
                                          DeviceBase? deviceKnownGood,
                                          bool verbose)
{
    // The purpose of this is to verify that every reading in deviceTest is in deviceKnownGood, and that
    // deviceKnownGood has no extra readings in the time interval spanned by deviceTest's readings.
    if (deviceTest == null || deviceKnownGood == null)
    {
        Console.WriteLine("Must specify both --deviceId and --baseDevice for compareHistory");
        return;
    }

    List<SensorReading> testReadings = await SensorReading.GetAllReadingsForDevice(sqlcn,
                                                                                   deviceTest.DeviceId,
                                                                                   null,
                                                                                   null,
                                                                                   null);
    Console.WriteLine("Device {0} ({1}): Fetched {2} readings, from {3} to {4}",
                        deviceTest.DeviceId,
                        deviceTest.Name,
                        testReadings.Count,
                        testReadings[testReadings.Count - 1].Timestamp,
                        testReadings[0].Timestamp);
    
    List<SensorReading> goodReadings = await SensorReading.GetAllReadingsForDevice(sqlcn,
                                                                                   deviceKnownGood.DeviceId,
                                                                                   null,
                                                                                   testReadings[testReadings.Count - 1].Timestamp,
                                                                                   testReadings[0].Timestamp);
    Console.WriteLine("Device {0} ({1}): Fetched {2} readings, from {3} to {4}",
                        deviceKnownGood.DeviceId,
                        deviceKnownGood.Name,
                        goodReadings.Count,
                        goodReadings[goodReadings.Count - 1].Timestamp,
                        goodReadings[0].Timestamp);
    
    // For simplicity, reverse both sets of readings to go oldest-to-newest.
    testReadings.Reverse();
    goodReadings.Reverse();

    int iTest = 0, iGood = 0;
    while (iTest < testReadings.Count && iGood < goodReadings.Count)
    {
        if (testReadings[iTest].Timestamp < goodReadings[iGood].Timestamp)
        {
            Console.WriteLine("-- Test reading missing in good readings: {0}/{1} @ {2}", testReadings[iTest].WaterHeightFeet, testReadings[iTest].WaterDischarge, testReadings[iTest].Timestamp);
            iTest++;
        }
        else if (goodReadings[iGood].Timestamp < testReadings[iTest].Timestamp)
        {
            Console.WriteLine("-- Good reading missing in test readings: {0}/{1} @ {2}", goodReadings[iGood].WaterHeightFeet, goodReadings[iGood].WaterDischarge, goodReadings[iGood].Timestamp);
            iGood++;
        }
        else
        {
            if (verbose)
            {
                Console.WriteLine("Comparing {0}/{1}@{2} vs {3}/{4}@{5}",
                                  testReadings[iTest].WaterHeight,
                                  testReadings[iTest].WaterDischarge,
                                  testReadings[iTest].Timestamp,
                                  goodReadings[iGood].WaterHeight,
                                  goodReadings[iGood].WaterDischarge,
                                  goodReadings[iGood].Timestamp);
            }
            UsgsSensorReadingComparer.CompareReadings(testReadings[iTest], goodReadings[iGood]);
            iTest++;
            iGood++;
        }
    }
    while (iTest < testReadings.Count)
    {
        Console.WriteLine("Extra test reading: {0}/{1} @ {2}", testReadings[iTest].WaterHeightFeet, testReadings[iTest].WaterDischarge, testReadings[iTest].Timestamp);
        iTest++;
    }
    while (iGood < goodReadings.Count)
    {
        Console.WriteLine("Extra good reading: {0}/{1} @ {2}", goodReadings[iGood].WaterHeightFeet, goodReadings[iGood].WaterDischarge, goodReadings[iGood].Timestamp);
        iTest++;
    }
}

async static Task ExecRunFetcher(SqlConnection sqlcn,
                                 UsgsSite? site,
                                 DeviceBase? device,
                                 DateTime startTime,
                                 DateTime endTime,
                                 bool verbose)
{
    if (site == null || device == null)
    {
        Console.WriteLine("Must specify site ID to fetch");
        return;
    }

    List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
    SensorLocationBase? location = locations.FirstOrDefault((l) => device.LocationId == l.Id);
    if (location == null)
    {
        Console.WriteLine("USGS Device {0} is not currently assigned to a location.", site.SiteId);
        return;
    }
    USGSWdfnFetcher.UpdateStatus updStat = new(new StringBuilder(), new StringBuilder())
    {
        errorCount = 0,
        dischargeReadingCount = 0,
        heightReadingCount = 0,
    };
    await USGSWdfnFetcher.UpdateUsgsGaugeForDateRange(sqlcn, site, device, location, startTime, endTime, "USGSTool 2026/02/02", updStat);

    Console.WriteLine("SUMMARY");
    Console.WriteLine("{0}", updStat.sbSummary.ToString());
    Console.WriteLine("DETAILS");
    Console.WriteLine("{0}", updStat.sbDetails.ToString());
    Console.WriteLine("RESULTS: {0} errors, {1} discharge readings, {2} height readings", updStat.errorCount, updStat.dischargeReadingCount, updStat.heightReadingCount);
}

async static Task ExecFetch(SqlConnection sqlcn, UsgsSite? site, DeviceBase? device, DateTime startTime, DateTime endTime, WdfnReadingType readingType, bool verbose)
{
    if (site == null || device == null)
    {
        Console.WriteLine("Must specify site ID to fetch");
        return;
    }
    List<WdfnReading>? readings = await site.FetchRawWdfnReadings(startTime, endTime, device, readingType);
    if (readings == null)
    {
        Console.WriteLine("FetchRawWDFNReadings returned null");
        return;
    }
    Console.WriteLine("Got {0} readings", readings.Count);
    foreach (WdfnReading reading in readings)
    {
        Console.WriteLine("{0} [UTC]: {1}", reading.Timestamp, reading.Value);
    }
}

static void CompareReadingLists(List<WdfnReading> fetchedReadings,
                                List<SensorReading> oldReadings,
                                WdfnReadingType readingType,
                                bool verbose,
                                Action<WdfnReading?, SensorReading?> mismatchAction)
{
    if (fetchedReadings.Count != oldReadings.Count)
    {
        Console.WriteLine("Warning: Reading count mismatch. Fetched {0} readings vs {1} old readings",
                          fetchedReadings.Count,
                          oldReadings.Count);
    }
    int iFetched = 0, iOld = 0;
    while (iFetched < fetchedReadings.Count || iOld < oldReadings.Count)
    {
        WdfnReading? fetched = null;
        SensorReading? old = null;
        double oldValue = 0;
        if (iFetched < fetchedReadings.Count)
        {
            fetched = fetchedReadings[iFetched];
        }
        if (iOld < oldReadings.Count)
        {
            old = oldReadings[iOld];
            if (readingType == WdfnReadingType.Discharge)
            {
                if (old.WaterDischarge.HasValue)
                {
                    oldValue = old.WaterDischarge.Value;
                }
            }
            else
            {
                if (old.WaterHeightFeet.HasValue)
                {
                    oldValue = old.WaterHeightFeet.Value;
                }
            }
        }
        if (fetched == null && old != null)
        {
            Console.WriteLine("Extra old at {0}: {1}", old.Timestamp, oldValue);
            mismatchAction(null, old);
            iOld++;
        }
        else if (fetched != null && old == null)
        {
            Console.WriteLine("Extra fetched at {0}: {1}", fetched.Timestamp, fetched.Value);
            mismatchAction(fetched, null);
            iFetched++;
        }
        else
        {
            if (fetched == null || old == null)
            {
                Console.WriteLine("Can't compare readings because one is null!");
                return;
            }
            if (fetched.Timestamp < old.Timestamp)
            {
                Console.WriteLine("Fetched < old: {0} < {1}", fetched.Timestamp, old.Timestamp);
                mismatchAction(fetched, null);
                iFetched++;
            }
            else if (fetched.Timestamp > old.Timestamp)
            {
                Console.WriteLine("Fetched > old: {0} > {1}", fetched.Timestamp, old.Timestamp);
                mismatchAction(null, old);
                iOld++;
            }
            else
            {
                // This is a special case -- we know there are situations where the old code returned
                // negative numbers (especially -999999) when the data was invalid. The new code appears
                // to return null in these situations, and that's ok (provided we handle the null appropriately...)
                if (oldValue == -999999 && !fetched.Value.HasValue)
                {
                    if (verbose)
                    {
                        Console.WriteLine("== {0}: {1} == {2}", fetched.Timestamp, fetched.Value, oldValue);
                    }
                }
                else if (fetched.Value != oldValue)
                {
                    Console.WriteLine("!! {0}: {1} != {2}", fetched.Timestamp, fetched.Value, oldValue);
                    mismatchAction(fetched, old);
                }
                else
                {
                    if (verbose)
                    {
                        Console.WriteLine("== {0}: {1} == {2}", fetched.Timestamp, fetched.Value, oldValue);
                    }
                }
                iOld++;
                iFetched++;
            }
        }
    }
}

async static Task ExecCompareWdfn(SqlConnection sqlcn,
                                  UsgsSite? site,
                                  DeviceBase? device,
                                  DateTime startTime,
                                  DateTime endTime,
                                  WdfnReadingType readingType,
                                  int readingLimit,
                                  bool verbose)
{
    if (site == null || device == null)
    {
        Console.WriteLine("Must specify site ID to fetch");
        return;
    }
    List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
    SensorLocationBase? location = locations.FirstOrDefault((l) => device.LocationId == l.Id);
    if (location == null)
    {
        Console.WriteLine("USGS Device {0} is not currently assigned to a location.", site.SiteId);
        return;
    }
    List<WdfnReading>? wdfnReadings = await site.FetchRawWdfnReadings(startTime, endTime, device, readingType, readingLimit);
    if (wdfnReadings == null)
    {
        Console.WriteLine("FetchRawWDFNReadings returned null");
        return;
    }
    UsgsSite.UsgsReadingData oldReadingData = await site.FetchUsgsReadings("UsgsTool", startTime, device, location);
    CompareReadingLists(wdfnReadings, oldReadingData.Readings, readingType, verbose, (WdfnReading? wdfnReading, SensorReading? oldReading) =>
    {
        Console.WriteLine("-- Got mismatch: {0} vs {1}", wdfnReading, oldReading);
    });
}

async static Task ExecReviewData(SqlConnection sqlcn,
                                 UsgsSite? site,
                                 DeviceBase? device,
                                 DateTime startTime,
                                 DateTime endTime,
                                 WdfnReadingType readingType,
                                 int readingLimit,
                                 bool verbose)
{
    if (site == null || device == null)
    {
        Console.WriteLine("Must specify site ID to review");
        return;
    }
    List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
    SensorLocationBase? location = locations.FirstOrDefault((l) => device.LocationId == l.Id);
    if (location == null)
    {
        Console.WriteLine("USGS Device {0} is not currently assigned to a location.", site.SiteId);
        return;
    }
    List<WdfnReading>? wdfnReadings = await site.FetchRawWdfnReadings(startTime, endTime, device, readingType, readingLimit);
    if (wdfnReadings == null)
    {
        Console.WriteLine("FetchRawWDFNReadings returned null");
        return;
    }
    // Bump the tail end of the interval forward by a second just in case it exactly matches
    // a reading; the GetReadings() method get readings in the interval [from, to).
    List<SensorReading> existingReadings
         = await SensorReading.GetAllReadingsForLocation(location.Id,
                                                         0,
                                                         startTime,
                                                         endTime + TimeSpan.FromSeconds(1));
    existingReadings = existingReadings.OrderBy(r => r.Timestamp).ToList();
    Console.WriteLine("Reviewing {0} old readings against {1} fetched readings", existingReadings.Count, wdfnReadings.Count);
    CompareReadingLists(wdfnReadings, existingReadings, readingType, verbose, (wdfnReading, oldReading) =>
    {
        Console.WriteLine("-- Got mismatch: {0} vs {1}", wdfnReading, oldReading);
    });
}

static DateTime ConvertToUtcAssumingRegionTime(DateTime userTime, RegionBase region, bool utc, DateTime defaultTime)
{
    if (userTime == DateTime.MinValue || userTime == DateTime.MaxValue)
    {
        return defaultTime;
    }
    else
    {
        if (utc)
        {
            return new DateTime(userTime.Year, userTime.Month, userTime.Day, userTime.Hour, userTime.Minute, userTime.Second, DateTimeKind.Utc);
        }
        else
        {
            // force this to be unspecified, even though it likely already is
            DateTime unspec = new DateTime(userTime.Year, userTime.Month, userTime.Day, userTime.Hour, userTime.Minute, userTime.Second, DateTimeKind.Unspecified);
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(region.WindowsTimeZone!);
            return TimeZoneInfo.ConvertTimeToUtc(unspec, tzi);
        }
    }
}

RootCommand cmd = new();

Option<bool> verboseOption = new Option<bool>("--verbose");
Option<bool> utcOption = new Option<bool>("--utc");
Option<int> regionOption = new Option<int>("--region") { DefaultValueFactory = parseResult => 1 };
Option<int> limitOption = new Option<int>("--limit") { DefaultValueFactory = parseResult => UsgsSite.DEFAULT_WDFN_READING_LIMIT };
Option<string> siteIdOption = new Option<string>("--siteId", "--siteid");
Option<string> deviceIdOption = new Option<string>("--deviceId", "--deviceid");
Option<string> baseDeviceOption = new Option<string>("--baseDevice");
Option<DateTime> startTimeOption = new Option<DateTime>("--startTime");
Option<DateTime> endTimeOption = new Option<DateTime>("--endTime");
Option<bool> dischargeOption = new Option<bool>("--discharge");
Option<bool> dryRunOption = new Option<bool>("--dryRun", "--dryrun");
cmd.Options.Add(verboseOption);
cmd.Options.Add(utcOption);
cmd.Options.Add(regionOption);
cmd.Options.Add(limitOption);
cmd.Options.Add(baseDeviceOption);
cmd.Options.Add(startTimeOption);
cmd.Options.Add(endTimeOption);
cmd.Options.Add(siteIdOption);
cmd.Options.Add(deviceIdOption);
cmd.Options.Add(dischargeOption);
cmd.Options.Add(dryRunOption);

cmd.Subcommands.Add(new Command("fetch"));
cmd.Subcommands.Add(new Command("runfetcher"));
cmd.Subcommands.Add(new Command("list"));
cmd.Subcommands.Add(new Command("compare"));
cmd.Subcommands.Add(new Command("review"));
cmd.Subcommands.Add(new Command("compareHistory"));

var parseResult = cmd.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
    {
        Console.WriteLine("ERROR: {0}", error.Message);
    }
    return;
}
var utc = parseResult.GetValue(utcOption);
int regionId = parseResult.GetValue(regionOption);
int limit = parseResult.GetValue(limitOption);
string? siteIdStr = parseResult.GetValue(siteIdOption);
string? deviceIdStr = parseResult.GetValue(deviceIdOption);
string? baseDeviceIdStr = parseResult.GetValue(baseDeviceOption);
DateTime rawStartTime = parseResult.GetValue(startTimeOption);
DateTime rawEndTime = parseResult.GetValue(endTimeOption);
bool verbose = parseResult.GetValue(verboseOption);
bool dryRun = parseResult.GetValue(dryRunOption);

if (!String.IsNullOrEmpty(siteIdStr) && !String.IsNullOrEmpty(deviceIdStr))
{
    Console.WriteLine("Cannot specify both siteId and deviceId option");
    return;
}

WdfnReadingType readingType = WdfnReadingType.Height;
if (parseResult.GetValue(dischargeOption))
{
    readingType = WdfnReadingType.Discharge;
}

FzConfig.Initialize();
using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
{
    await sqlcn.OpenAsync();
    RegionBase region = await RegionBase.GetRegionAsync(sqlcn, regionId);
    if (region == null)
    {
        Console.WriteLine("Region {0} is invalid", regionId);
        return;
    }
    DateTime endTime = ConvertToUtcAssumingRegionTime(rawEndTime, region, utc, DateTime.UtcNow);
    DateTime startTime = ConvertToUtcAssumingRegionTime(rawStartTime, region, utc, endTime - TimeSpan.FromDays(1));
    if (verbose)
    {
        Console.WriteLine("Start time: {0}", XmlConvert.ToString(startTime, XmlDateTimeSerializationMode.RoundtripKind));
        Console.WriteLine("End time: {0}", XmlConvert.ToString(endTime, XmlDateTimeSerializationMode.RoundtripKind));
    }

    List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);
    List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
    UsgsSite? site = null;
    DeviceBase? device = null;
    int deviceId;
    if (deviceIdStr != null)
    {
        if (!Int32.TryParse(deviceIdStr, out deviceId))
        {
            Console.WriteLine("'{0}' is not a valid site ID string", deviceIdStr);
            return;
        }
        device = devices.FirstOrDefault((d) => d.DeviceId == deviceId);
        if (device == null)
        {
            Console.WriteLine("Device {0} does not exist", deviceIdStr);
            return;
        }
        if (device.DeviceTypeId != DeviceTypeIds.Usgs && device.DeviceTypeId != DeviceTypeIds.UsgsTestingDevice)
        {
            Console.WriteLine("Device {0} ({1}) is not a USGS device", deviceIdStr, device.Name);
            return;
        }
        site = usgsSites.FirstOrDefault((s) => s.SiteId == device.UsgsSiteId);
        if (site == null)
        {
            Console.WriteLine("Device {0} ({1}) does not have a matching USGS Site", deviceIdStr, device.Name);
            return;
        }
    }
    int siteId;
    if (siteIdStr != null)
    {
        if (!Int32.TryParse(siteIdStr, out siteId))
        {
            Console.WriteLine("'{0}' is not a valid site ID string", siteIdStr);
            return;
        }
        site = usgsSites.FirstOrDefault(s => s.SiteId == siteId);
        if (site == null)
        {
            Console.WriteLine("Site {0} is not a known USGS site", siteId);
            return;
        }
        device = devices.FirstOrDefault(d => !d.IsDeleted && d.UsgsSiteId == siteId);
        if (device == null)
        {
            Console.WriteLine("USGS Site {0} does not have a matching device", siteId);
            return;
        }
    }
    if (device != null && site != null && verbose)
    {
        Console.WriteLine("DEVICE: {0} ({1}), site {2} ({3})", device.DeviceId, device.Name, site.SiteId, site.SiteName);
    }

    int baseDeviceId = 0;
    DeviceBase? baseDevice = null;
    if (baseDeviceIdStr != null)
    {
        if (!Int32.TryParse(baseDeviceIdStr, out baseDeviceId))
        {
            Console.WriteLine("Base device: {0} is not a device ID", baseDeviceIdStr);
            return;
        }
        baseDevice = devices.FirstOrDefault(d => d.DeviceId == baseDeviceId);
        if (baseDevice == null)
        {
            Console.WriteLine("Base device {0} does not exist", baseDeviceIdStr);
            return;
        }

    }

    if (dryRun)
    {
        Console.WriteLine("Dry Run flag specified.  Exiting");
        return;
    }

    switch (parseResult.CommandResult.Command.Name)
    {
        case "list":
            await ExecList(usgsSites, regionId);
            break;
        case "fetch":
            await ExecFetch(sqlcn, site, device, startTime, endTime, readingType, verbose);
            break;
        case "runfetcher":
            await ExecRunFetcher(sqlcn, site, device, startTime, endTime, verbose);
            break;
        case "compare":
            await ExecCompareWdfn(sqlcn, site, device, startTime, endTime, readingType, limit, verbose);
            break;
        case "review":
            await ExecReviewData(sqlcn, site, device, startTime, endTime, readingType, limit, verbose);
            break;
        case "compareHistory":
            await ExecCompareGaugeHistory(sqlcn, device, baseDevice, verbose);
            break;
    }
    sqlcn.Close();
}
