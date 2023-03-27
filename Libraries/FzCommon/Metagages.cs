namespace FzCommon
{
    public class Metagage
    {
        public string Id;
        public string SiteIds;
        public string Name;
        public string ShortName;
        public double StageOne;
        public double StageTwo;
    }

    public class Metagages
    {
        //$ TODO: Support different regions
        //$ TODO: Generalize this if there's ever more than one.
        public static readonly int MetagageRegion = 1;
        public static readonly string[] MetagageIds = {"USGS-SF17/USGS-NF10/USGS-MF11"};
        public static readonly string[] MetagageSiteIds = {"GARW1-SNQW1-TANW1"};
        public static readonly string[] MetagageNames = {"Sum of the 3 forks"};
        public static readonly string[] MetagageShortNames = {"Forks"};
        public static readonly double[] MetagageStageOnes = {10000};
        public static readonly double[] MetagageStageTwos = {12000};

        public static string GetShortName(string gage)
        {
            for (int i = 0; i < MetagageIds.Length; i++)
            {
                if (MetagageIds[i] == gage)
                {
                    return MetagageShortNames[i];
                }
            }
            return null;
        }

        public static Metagage? FindMatchingMetagage(string[] subGageIds)
        {
            Array.Sort(subGageIds);
            for (int i = 0; i < MetagageIds.Length; i++)
            {
                string[] metaIds = MetagageIds[i].Split("/");
                Array.Sort(metaIds);
                bool match = true;
                for (int idIndex = 0; idIndex < subGageIds.Length; idIndex++)
                {
                    if (metaIds[idIndex] != subGageIds[idIndex])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return new Metagage()
                    {
                        Id = MetagageIds[i],
                        SiteIds = MetagageSiteIds[i],
                        Name = MetagageNames[i],
                        ShortName = MetagageShortNames[i],
                        StageOne = MetagageStageOnes[i],
                        StageTwo = MetagageStageTwos[i],
                    };
                }
            }
            return null;
        }
    }

    public class MetagageHelpers
    {
        // This only sums WaterDischarge; summing WaterHeight sort of doesn't make any sense physically...
        // Also note: this assumes readings are sorted newest-first.
        public static List<SensorReading> SumReadings(List<Queue<SensorReading>> subReadings)
        {
            List<SensorReading> sums = new List<SensorReading>();
            SensorReading subReading;
            while (subReadings[0].TryDequeue(out subReading))
            {
                SensorReading sumReading = new SensorReading()
                {
                    Timestamp = subReading.Timestamp,
                    WaterDischarge = subReading.WaterDischarge,
                };
                bool skip = false;
                for (int i = 1; i < subReadings.Count; i++)
                {
                    SensorReading candidate;
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
                    sums.Add(sumReading);
                }
            }
            return sums;
        }
    }
}
