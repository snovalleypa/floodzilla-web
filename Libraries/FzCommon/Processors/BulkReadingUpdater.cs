using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using ObjectsComparator.Helpers.Extensions;

namespace FzCommon.Processors
{
    public class UpdaterLogItem(int readingId, bool changed, string? diff)
    {
        public int ReadingId = readingId;
        public bool Changed = changed;
        public string? Diff = diff;
    }

    public abstract class BulkReadingUpdater(SensorLocationBase location, string name, string cachePath, string logPath, int batchSize, int batchCount)
    {
        public class BRUStatus
        {
            public List<int> RemainingReadingIds = [];
        }

        // This should fill the status's reading ID list with reading IDs in the order that they
        // expect to be processed (i.e. if you want to do newest first, return those readings first).
        public abstract Task<BRUStatus> InitializeStatus(SqlConnection sqlcn);

        public abstract bool UpdateReading(SensorReading reading);

        public async Task Run(SqlConnection sqlcn)
        {
            this.m_cancelled = false;
            this.m_startTime = DateTime.Now; // this can just be in local time, it's just for logfile naming
            BRUStatus status = await this.GetStatus(sqlcn);
            if (status.RemainingReadingIds.Count == 0)
            {
                Console.WriteLine("No readings loaded, nothing to do, exiting");
                return;
            }
            if (this.IsCancelled())
            {
                return;
            }
            for (int batchesSoFar = 0; batchesSoFar < this.m_batchCount; batchesSoFar++)
            {
                Console.Write("Starting batch {0} of {1}...", batchesSoFar + 1, this.m_batchCount);
                List<int> readingsForThisBatch = [];
                for (int i = 0; i < status.RemainingReadingIds.Count && i < this.m_batchSize; i++)
                {
                    readingsForThisBatch.Add(status.RemainingReadingIds[i]);
                }
                var (originalReadings, updatedReadings) = await LoadReadingSet(sqlcn, readingsForThisBatch);

                StringBuilder batchDiff = new();
                using (SqlTransaction xaction = sqlcn.BeginTransaction())
                {
                    for (int readingsSoFar = 0; readingsSoFar < originalReadings.Count; readingsSoFar++)
                    {
                        if (this.IsCancelled())
                        {
                            Console.WriteLine("Rolling back transaction...");
                            await xaction.RollbackAsync();
                            return;
                        }

                        int readingId = status.RemainingReadingIds[readingsSoFar];
                        if (this.UpdateReading(updatedReadings[readingsSoFar]))
                        {
                            await updatedReadings[readingsSoFar].BulkSave(sqlcn, xaction);

                            ObjectDiff diff = ObjectDiffer.DiffSensorReadings(originalReadings[readingsSoFar], updatedReadings[readingsSoFar]);
#if VERBOSE
                            Console.WriteLine(JsonConvert.SerializeObject(diff, Formatting.Indented));
#endif
                            batchDiff.AppendFormat("{0}\r\n", JsonConvert.SerializeObject(diff, Formatting.None));
                        }
                    }
                    Console.Write("Committing batch...");
                    await xaction.CommitAsync();
                    Console.WriteLine("Done");
                    this.AppendDiffLog(batchDiff.ToString());
                    status.RemainingReadingIds.RemoveRange(0, originalReadings.Count);
                    await this.SaveStatus(status);
                }
            }
        }

        public void Cancel()
        {
            this.m_cancelled = true;
        }

        // Ad-hoc query with what looks like SQL injection! But it's safe because the string I'm building is
        // of a known format.
        const string READING_LIST_QUERY_FORMAT = "SELECT * from SensorReadings WHERE Id IN ({0})";
        private static async Task<(List<SensorReading> original, List<SensorReading> updated)> LoadReadingSet(SqlConnection sqlcn, List<int> readingIds)
        {
            List<SensorReading> ret1 = [], ret2 = [];
            string query = String.Format(READING_LIST_QUERY_FORMAT, String.Join(',', readingIds));
            using (SqlCommand cmd = new(query, sqlcn))
            {
                using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        // Because I'm not sure I trust our "clone a reading" constructor, this instantiates
                        // the entire set of readings twice.
                        ret1.Add(BulkSensorReading.BulkInstantiateFromReader(rdr));
                        ret2.Add(BulkSensorReading.BulkInstantiateFromReader(rdr));
                    }
                }
            }
            return (ret1, ret2);
        }

        private async Task<BRUStatus> GetStatus(SqlConnection sqlcn)
        {
            BRUStatus? status = null;
            try
            {
                using FileStream fs = new(GetCachePath(), FileMode.Open);
                using StreamReader sr = new(fs);
                using JsonTextReader jr = new(sr);
                status = new JsonSerializer().Deserialize<BRUStatus>(jr);
                if (status != null && status.RemainingReadingIds.IsEmpty())
                {
                    Console.WriteLine("Empty status found -- resetting");
                    status = null;
                }
            }
            catch
            {
                // do nothing, just start from scratch
            }
            if (status == null)
            {
                Console.WriteLine("Saved status not found -- intializing");
                status = await InitializeStatus(sqlcn);
            }
            if (this.IsCancelled())
            {
                return new BRUStatus();
            }
            await SaveStatus(status);
            Console.WriteLine("Starting run: Loaded {0} reading IDs", status.RemainingReadingIds.Count);
            return status;
        }

        private async Task SaveStatus(BRUStatus status)
        {
            try
            {
                using FileStream fs = new(GetCachePath(), FileMode.Create);
                using StreamWriter sw = new(fs);
                new JsonSerializer().Serialize(sw, status);
            }
            catch
            {
                Console.WriteLine("WARNING: Unable to save status file...");
                // nothing else to do for now...
            }
        }

        private string GetCachePath()
        {
            return String.Format("{0}\\{1}-loc-{2}-status-cache.json", m_cachePath, m_name, m_location.Id);
        }

        private string GetLogTimeStamp()
        {
            return m_startTime.ToString("yyyy-MM-dd");
        }

        private string GetLogPath()
        {
            return String.Format("{0}\\{1}-loc-{2}-updates-{3}.json", m_logPath, m_name, m_location.Id, GetLogTimeStamp());
        }

        private void AppendDiffLog(string logText)
        {
            using FileStream fs = new(GetLogPath(), FileMode.Append, FileAccess.Write);
            using StreamWriter sr = new(fs);
            sr.Write(logText);
            sr.Close();
            fs.Close();
        }

        protected readonly SensorLocationBase m_location = location;
        private readonly string m_name = name;
        private readonly string m_cachePath = cachePath;
        private readonly string m_logPath = logPath;
        private readonly int m_batchSize = batchSize;
        private readonly int m_batchCount = batchCount;
        private bool m_cancelled;
        private DateTime m_startTime;
        protected bool IsCancelled() { return m_cancelled; }

        // Cheeky way to make InstantiateFromReader available...
        internal class BulkSensorReading : SensorReading
        {
            internal static SensorReading BulkInstantiateFromReader(SqlDataReader dr, Func<SensorReading>? factory = null)
            {
                return SensorReading.InstantiateFromReader(dr, factory);
            }
        }
    }
}
