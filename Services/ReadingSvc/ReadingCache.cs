using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using FzCommon;

namespace ReadingSvc
{
    public class ReadingCache
    {
        // In minutes...
        public const int OutageThreshold = 120;
        public const int ExpectedInterval = 15;
        public const int ReadingWindow = 2;
                
        public ReadingCache()
        {
            m_readingCache = new Dictionary<string, ReadingCacheEntry>();
            m_lock = new object();
        }

        public async Task<List<SensorReading>> GetReadingsForLocation(int locationId,
                                                                      string locationPublicId,
                                                                      bool includeDeleted,
                                                                      bool includeMissing,
                                                                      bool getMinimalReadings,
                                                                      int? readingCount,
                                                                      DateTime? fromDateTimeUtc,
                                                                      DateTime? toDateTimeUtc,
                                                                      int skipCount = 0,
                                                                      int lastReadingId = 0)
        {

            // SHORT-TERM: if there's a cache entry, use it; otherwise, just do the query directly.
            ReadingCacheEntry entry = await this.GetEntry(locationId, locationPublicId, true);
            if (entry == null)
            {
                // SHORT-TERM: For now, we don't support 'missing' readings in non-cached scenarios.
                if (includeMissing)
                {
                    throw new ApplicationException("Support for 'missing' readings is NYI...");
                }
                
                return await this.FetchReadingsForLocation(locationId,
                                                           includeDeleted,
                                                           false,
                                                           getMinimalReadings,
                                                           readingCount,
                                                           fromDateTimeUtc,
                                                           toDateTimeUtc,
                                                           skipCount,
                                                           lastReadingId);
            }
            List<SensorReading> ret = new List<SensorReading>();
            int count = 0;
            int countSkipped = 0;
            foreach (SensorReading candidate in entry.AllReadings)
            {
                if (!includeDeleted && candidate.IsDeleted)
                {
                    continue;
                }

                // The Id<0 check is kind of a hack, but I don't (yet) want to put anything into SensorReading
                // to indicate that it's a fake reading
                if (!includeMissing && (candidate.Id < 0))
                {
                    continue;
                }
                if (toDateTimeUtc.HasValue && candidate.Timestamp > toDateTimeUtc.Value)
                {
                    continue;
                }
                if (fromDateTimeUtc.HasValue && candidate.Timestamp < fromDateTimeUtc.Value)
                {
                    // Because readings are loaded newest-first, if we get a reading that's before
                    // our start date, we're done.
                    break;
                }
                if (lastReadingId > 0 && candidate.Id <= lastReadingId)
                {
                    break;
                }

                if (skipCount > 0 && countSkipped <= skipCount)
                {
                    countSkipped++;
                    continue;
                }

                ret.Add(candidate);
                count++;
                if (count >= readingCount)
                {
                    break;
                }
            }
            return ret;
        }

        public async Task<List<SensorOutage>> GetOutagesForLocation(int locationId, string locationPublicId)
        {
            ReadingCacheEntry entry = await this.GetEntry(locationId, locationPublicId, false);
            if (entry == null)
            {
                return null;
            }
            return entry.Outages;
        }

        public async Task<SensorReading> GetSensorReading(string locationPublicId, int readingId)
        {
            ReadingCacheEntry entry = await this.GetEntry(locationPublicId);
            if (entry == null)
            {
                return null;
            }
            foreach (SensorReading sr in entry.AllReadings)
            {
                if (sr.Id == readingId)
                {
                    return sr;
                }
            }
            return null;
        }

        //$ TODO: write to DB
        public async Task<bool> MarkReadingAsDeleted(string locationPublicId, int readingId, SensorReading sr)
        {
            sr.IsDeleted = true;
            return true;
        }        

        //$ TODO: write to DB
        public async Task<bool> MarkReadingAsUndeleted(string locationPublicId, int readingId, SensorReading sr)
        {
            sr.IsDeleted = false;
            return true;
        }

        private async Task<List<SensorReading>> FetchReadingsForLocation(int locationId,
                                                                         bool includeDeleted,
                                                                         bool includeMissing,
                                                                         bool getMinimalReadings,
                                                                         int? readingCount,
                                                                         DateTime? fromDateTimeUtc,
                                                                         DateTime? toDateTimeUtc,
                                                                         int skipCount = 0,
                                                                         int lastReadingId = 0)
        {
            //$ TODO: Support includeMissing
            if (includeMissing)
            {
                throw new ApplicationException("Support for 'missing' readings is NYI...");
            }

            if (includeDeleted)
            {
                return await SensorReading.GetAllReadingsForLocation(locationId,
                                                                     readingCount,
                                                                     fromDateTimeUtc,
                                                                     toDateTimeUtc,
                                                                     skipCount,
                                                                     lastReadingId);
            }
            else
            {
                if (getMinimalReadings)
                {
                    if (skipCount > 0)
                    {
                        throw new ApplicationException("Skip count isn't supported for minimal readings");
                    }
                    if (lastReadingId > 0)
                    {
                        throw new ApplicationException("Last Reading isn't supported for minimal readings");
                    }
                    return await SensorReading.GetMinimalReadingsForLocation(locationId,
                                                                             readingCount,
                                                                             fromDateTimeUtc,
                                                                             toDateTimeUtc);
                }
                else
                {
                    return await SensorReading.GetReadingsForLocation(locationId,
                                                                      readingCount,
                                                                      fromDateTimeUtc,
                                                                      toDateTimeUtc,
                                                                      skipCount,
                                                                      lastReadingId);
                }
            }
        }

        private async Task<ReadingCacheEntry> FetchEntry(int locationId, string locationPublicId)
        {
            ReadingCacheEntry entry = null;
            //$ TODO: this can double-fetch.  build a better lock system.
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                entry = new ReadingCacheEntry()
                {
                    Id = locationId,
                };
                List<SensorReading> allReadings = await SensorReading.GetAllReadingsForLocation(locationId, null, null, null, 0, 0);
                this.InitializeCacheEntry(entry, allReadings);
            }
            lock (m_lock)
            {
                m_readingCache[locationPublicId] = entry;
            }
            return entry;
        }

        private async Task<ReadingCacheEntry> GetEntry(int locationId, string locationPublicId, bool onlyIfAlreadyCached)
        {
            if (m_readingCache.ContainsKey(locationPublicId))
            {
                return m_readingCache[locationPublicId];
            }
            if (onlyIfAlreadyCached)
            {
                //$ TODO: lazy fetch the entry
                //$ don't wait for this.
                //$ FetchEntry(locationId, locationPublicId);
                return null;
            }
            return await FetchEntry(locationId, locationPublicId);
        }

        //$ TODO: Make this fetch the location obj, i guess, instead of just throwing
        private async Task<ReadingCacheEntry> GetEntry(string locationPublicId)
        {
            if (m_readingCache.ContainsKey(locationPublicId))
            {
                return m_readingCache[locationPublicId];
            }
            throw new ApplicationException("Must initialize cache via location ID first...");
        }

        //$ TODO: This isn't really a 'cache' operation; rework this when the caching system gets rebuilt
        private void InitializeCacheEntry(ReadingCacheEntry entry, List<SensorReading> allReadings)
        {
            entry.Outages = new List<SensorOutage>();
            entry.AllReadings = new List<SensorReading>();

            // This is a little tricky because readings are stored latest-first.  Note also
            // that we assume the latest reading is effectively up-to-date; we don't create
            // missing readings or outages for current data, only past data.
            SensorReading lastReading = allReadings[0];
            SensorReading lastNonDeleted = lastReading; // not necessarily true, but close enough.
            entry.AllReadings.Add(lastReading);
            int missingId = -1;

            for (int i = 1; i < allReadings.Count; i++)
            {
                SensorReading cur = allReadings[i];
                TimeSpan interval = lastReading.Timestamp.Subtract(cur.Timestamp);
                if (interval.TotalMinutes > ExpectedInterval + ReadingWindow)
                {
                    // We either have a missed reading or an outage.
                    if (interval.TotalMinutes > OutageThreshold + ReadingWindow)
                    {
                        SensorOutage outage = new SensorOutage()
                        {
                            LastIdBeforeOutage = cur.Id,
                            LastTimestampBeforeOutage = cur.Timestamp,
                            FirstIdAfterOutage = lastReading.Id,
                            FirstTimestampAfterOutage = lastReading.Timestamp,
                        };
                        entry.Outages.Add(outage);
                    }
                    else
                    {
                        int missingCount = (int)((interval.TotalMinutes + (double)ReadingWindow) / (double)ExpectedInterval) - 1;
                        DateTime timestamp = lastReading.Timestamp;

                        //$ TODO: lerp readings?
                        for (int iMissing = 0; iMissing < missingCount; iMissing++)
                        {
                            timestamp = timestamp.AddMinutes(-ExpectedInterval);

                            // Because we there could be a little clock skew, we double-check that we're
                            // not creating a reading too close to our next one.
                            if (Math.Abs(timestamp.Subtract(cur.Timestamp).TotalMinutes) <= (double)ReadingWindow)
                            {
                                break;
                            }
                            SensorReading missing = new SensorReading(lastNonDeleted);
                            missing.Id = missingId--;
                            missing.Timestamp = timestamp;
                            missing.IsDeleted = false;
                            entry.AllReadings.Add(missing);
                        }
                    }
                }
                entry.AllReadings.Add(cur);
                lastReading = cur;
                if (!cur.IsDeleted)
                {
                    lastNonDeleted = cur;
                }
            }
        }
        
        private Dictionary<string, ReadingCacheEntry> m_readingCache;
        private object m_lock;
    }

    public class SensorOutage
    {
        public int LastIdBeforeOutage;
        public DateTime LastTimestampBeforeOutage;
        public int FirstIdAfterOutage;
        public DateTime FirstTimestampAfterOutage;
    }

    internal class ReadingCacheEntry
    {
        public int Id;
        public List<SensorReading> AllReadings;
        public List<SensorOutage> Outages;
    }
}

