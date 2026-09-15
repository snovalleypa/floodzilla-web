#define VERBOSE

using System.Data;
using FzCommon;
using FzCommon.Processors;
using Microsoft.Data.SqlClient;

namespace FixupBenchmarkChanges
{
    public class BenchmarkFixer(SensorLocationBase location, string cachePath, string logPath) : BulkReadingUpdater(location, "BenchmarkFixer", cachePath, logPath, BATCH_SIZE, BATCH_COUNT)
    {
        const int BATCH_SIZE = 50;
        const int BATCH_COUNT = 50;

        // Ad-hoc query, since this is a specialized thing and I don't want to bother with a sproc
        const string INTIALIZE_QUERY =
        @"SELECT sr.Id AS ReadingId, sr.BenchmarkElevation AS ReadingBenchmark, l.BenchmarkElevation / 12.0 AS LocationBenchmark 
            FROM SensorReadings sr JOIN Locations l ON l.Id = sr.LocationId
            WHERE L.Id = @locationId AND 
                sr.IsDeleted <> 1 AND
                ABS(l.BenchmarkElevation / 12.0 - sr.BenchmarkElevation) > .0001
            ORDER BY sr.Id DESC";
        public override async Task<BRUStatus> InitializeStatus(SqlConnection sqlcn)
        {
            BRUStatus status = new();
            using (SqlCommand cmd = new(INTIALIZE_QUERY, sqlcn))
            {
                cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = m_location.Id;
                cmd.CommandTimeout = 60 * 60; // This is overkill, but...
                using SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    if (this.IsCancelled())
                    {
                        return new BRUStatus();
                    }
                    int readingId = SqlHelper.Read<int>(rdr, "ReadingId");
#if VERBOSE
                    double readingBenchmark = SqlHelper.Read<double>(rdr, "ReadingBenchmark");
                    double locationBenchmark = SqlHelper.Read<double>(rdr, "LocationBenchmark");
                    Console.WriteLine("READING {0}: benchmark is {1} vs location {2}", readingId, readingBenchmark, locationBenchmark);
#endif
                    status.RemainingReadingIds.Add(readingId);
                }
            }
            return status;
        }

        public override bool UpdateReading(SensorReading reading)
        {
            if (!m_location.BenchmarkElevation.HasValue || !reading.BenchmarkElevation.HasValue)
            {
                return false;
            }
            if (reading.BenchmarkElevation == m_location.BenchmarkElevation)
            {
#if VERBOSE
                Console.WriteLine("READING {0}: Skipping, benchmark unchanged", reading.Id);
#endif
                return false;
            }
            double benchDiff = m_location.BenchmarkElevation.Value - reading.BenchmarkElevation.Value;
            double benchDiffInches = benchDiff * 12.0;
            reading.BenchmarkElevation = m_location.BenchmarkElevation;
            reading.RawWaterHeightFeet += benchDiff;
            reading.GroundHeightFeet += benchDiff;
            reading.WaterHeightFeet += benchDiff;
            reading.GroundHeight += benchDiffInches;
            reading.WaterHeight += benchDiffInches;
            reading.RawWaterHeight += benchDiffInches;
            return true;
        }
    }
}