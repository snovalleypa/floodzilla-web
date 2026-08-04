using System.ComponentModel;
using System.Data;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Security;
using Azure.Storage.Blobs.Models;
using FzCommon;
using Microsoft.Data.SqlClient;

// This uses an ad-hoc query because I don't really want to bake this into a sproc -- it should
// only be necessary from this tool.
const string ALL_DUPSETS_QUERY = "SELECT COUNT(Id) as Count, LocationId, Timestamp FROM SensorReadings GROUP BY LocationId, Timestamp HAVING COUNT(Id) > 1 ORDER BY LocationId, Timestamp";
async Task<Dictionary<int, List<DupReadingSet>>> GetDupSets(SqlConnection sqlcn)
{
    Dictionary<int, List<DupReadingSet>> ret = [];
    int count = 0;
    using (SqlCommand cmd = new(ALL_DUPSETS_QUERY, sqlcn))
    {
        using SqlDataReader rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            DupReadingSet dup = DupReadingSet.FromReader(rdr);
            if (!ret.ContainsKey(dup.LocationId))
            {
                ret[dup.LocationId] = [];
            }
            count++;
            ret[dup.LocationId].Add(dup);
        }
    }
    Console.WriteLine("Loaded {0} sets of duplicate readings...", count);
    return ret;
}

async Task<List<SensorReading>> GetAllPossibleSensorReadings(SqlConnection sqlcn, List<DupReadingSet> allDupsForLocation)
{
    int locationId = -1;
    List<SensorReading> ret = [];
    DateTime endTime = DateTime.MinValue;
    DateTime startTime = DateTime.MaxValue;
    foreach (DupReadingSet dup in allDupsForLocation)
    {
        if (locationId == -1)
        {
            locationId = dup.LocationId;
        }
        else
        {
            if (locationId != dup.LocationId)
            {
                throw new ApplicationException("DupReadingSet had wrong locationId!?");
            }
        }
        // Be paranoid, don't assume these are in timestamp order (although they should be)...
        if (endTime < dup.Timestamp)
        {
            endTime = dup.Timestamp;
        }
        if (startTime > dup.Timestamp)
        {
            startTime = dup.Timestamp;
        }
    }
    List<SensorReading> readings = await SensorReading.GetAllReadingsForLocation(sqlcn, locationId, null, startTime, endTime);
    foreach (SensorReading reading in readings)
    {
        ret.Add(reading);
    }
    // Sort by Timestamp ascending, Id ascending to make finding matches easier.
    ret.Sort((a, b) =>
    {
        if (a.Timestamp == b.Timestamp)
        {
            return a.Id - b.Id;
        }
        return (int)((a.Timestamp - b.Timestamp).TotalMilliseconds);
    });
    return ret;
}

List<SensorReading> GetMatchingReadings(List<SensorReading> allReadings, DupReadingSet set)
{
    List<SensorReading> ret = [];
    for (int i = 0; i < allReadings.Count; i++)
    {
        if (allReadings[i].Timestamp > set.Timestamp)
        {
            return ret;
        }
        if (allReadings[i].Timestamp == set.Timestamp)
        {
            ret.Add(allReadings[i]);
        }
    }
    return ret;
}

SensorReading FindBestReadingInSet(List<SensorReading> readings)
{
    int bestIndex = -1;
    for (int i = 0; i < readings.Count; i++)
    {
        // Current criteria: earliest (lowest-ID) reading that has both WaterHeight and WaterDischarge is the "best"
        if (readings[i].WaterHeight.HasValue && readings[i].WaterDischarge.HasValue)
        {
            bestIndex = i;
            break;
        }
    }
    if (bestIndex == -1)
    {
        throw new ApplicationException(String.Format("ERROR: Reading set for {0} @ {1} doesn't have a complete reading", readings[0].LocationId, readings[0].Timestamp));
    }
    return readings[bestIndex];
}

const int COUNT_PER_BATCH = 5;
int dupSetCount = 0;
async Task ProcessReadingSets(SqlConnection sqlcn)
{
    Dictionary<int, List<DupReadingSet>> dups = await GetDupSets(sqlcn);
    foreach (int locId in dups.Keys)
    {
        List<DupReadingSet> sets = dups[locId];
        List<SensorReading> allReadings = await GetAllPossibleSensorReadings(sqlcn, sets);

        foreach (DupReadingSet set in sets)
        {
            List<SensorReading> readings = GetMatchingReadings(allReadings, set);
            SensorReading best = FindBestReadingInSet(readings);
            // NOTE: Yes, I'm using String.Format to build a SQL query.  This is ok because (a) I control the format and
            // the inputs, and (b) it's not going to be executed directly; it's going to be manually executed by a human
            // and verified before and after...
            string timestampString = set.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Console.WriteLine();
            Console.WriteLine("SELECT Id,Timestamp,LocationId,DeviceId,DistanceReading,WaterHeight,WaterHeightFeet,WaterDischarge,IsDeleted FROM SensorReadings WHERE LocationId={0} AND Timestamp='{1}' ORDER BY Id ASC",
                              set.LocationId, timestampString);
            Console.WriteLine("-- Expected count: {0}", readings.Count);
            Console.WriteLine("-- Expected readings: {0}", String.Join(',', readings.Select(r => r.Id)));
            Console.WriteLine("-- DELETE FROM SensorReadings WHERE LocationId={0} AND Timestamp='{1}' AND Id <> {2}",
                              set.LocationId, timestampString, best.Id);
            Console.WriteLine("SELECT Id,Timestamp,LocationId,DeviceId,DistanceReading,WaterHeight,WaterHeightFeet,WaterDischarge,IsDeleted FROM SensorReadings WHERE LocationId={0} AND Timestamp='{1}' AND Id <> {2}",
                              set.LocationId, timestampString, best.Id);
            Console.WriteLine();
            Console.WriteLine("--================================================================");
            if (++dupSetCount >= COUNT_PER_BATCH)
            {
                return;
            }
        }
    }
}

Console.WriteLine("--================================================================");
FzConfig.Initialize();
using (SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
{
    await sqlcn.OpenAsync();
    await ProcessReadingSets(sqlcn);
    await sqlcn.CloseAsync();
}

public class DupReadingSet
{
    public int Count;
    public int LocationId;
    public DateTime Timestamp;

    public static DupReadingSet FromReader(SqlDataReader dr)
    {
        return new()
        {
            Count = SqlHelper.Read<int>(dr, "Count"),
            LocationId = SqlHelper.Read<int>(dr, "LocationId"),
            Timestamp = SqlHelper.Read<DateTime>(dr, "Timestamp"),
        };
    }
}
