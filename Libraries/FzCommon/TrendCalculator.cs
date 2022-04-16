namespace FzCommon
{
    public enum LevelTrend
    {
        Offline,
        Rising,
        Steady,
        Falling,
    }

    // This is pretty experimental for now.  All numbers are returned as a delta value per hour.
    // For example, if your extractor function fetches WaterDischarge, the trend numbers will be
    // in delta-CFS per hour.
    public class Trends
    {
        public double?[] TrendValues    { get; set; }
        public double? TrendValue       { get; set; }

        public const int DesiredReadingCount = 5;

        // Readings further behind the latest than this threshold are ignored for trend calculation.  If that makes sense.
        public const int AgeThresholdMinutes = 4 * 60;

        public const int PredictionIntervalMinutes = 15;
        public const int PredictionCount = 4 * 6; // 6 hours' worth

        //$ TODO(daves): what's the right number here?
        // This number is in feet per hour...
        public const double ThresholdSlope = .07;
        public LevelTrend GetLevelTrend()
        {
            if (this.TrendValue == null)
            {
                return LevelTrend.Offline;
            }
            if (this.TrendValue < -ThresholdSlope)
            {
                return LevelTrend.Falling;
            }
            else if (this.TrendValue > ThresholdSlope)
            {
                return LevelTrend.Rising;
            }
            else
            {
                return LevelTrend.Steady;
            }            
        }
    }

    public class TrendCalculator
    {
        public static Trends CalculateWaterTrends(List<SensorReading> readings)
        {
            return CalculateTrends(readings, r => r.WaterHeightFeet);
        }

        public static Trends CalculateDischargeTrends(List<SensorReading> readings)
        {
            return CalculateTrends(readings, r => r.WaterDischarge);
        }

        // Expects readings to be sorted newest-first
        public static Trends CalculateTrends(List<SensorReading> readings, Func<SensorReading, double?> extractor)
        {
            Trends trends = new Trends();
            if (readings == null || readings.Count < 2)
            {
                return trends;
            }
            double? lastVal = extractor(readings[0]);
            if (!lastVal.HasValue)
            {
                return trends;
            }
            DateTime lastTime = readings[0].Timestamp;
            trends.TrendValues = new double?[Trends.DesiredReadingCount - 1];

            double? candidate;
            double? total = 0;
            for (int i = 1; i < Trends.DesiredReadingCount;i++)
            {
                candidate = (readings.Count > i) ? extractor(readings[i]) : null;
                if (candidate.HasValue)
                {
                    TimeSpan difference = lastTime.Subtract(readings[i].Timestamp);

                    if (difference.TotalMinutes > Trends.AgeThresholdMinutes)
                    {
                        break;
                    }
                    trends.TrendValues[i - 1] = (lastVal - candidate) / difference.TotalHours;
                    total += trends.TrendValues[i - 1];

                    // This will end up being the oldest valid candidate slope
                    trends.TrendValue = trends.TrendValues[i - 1];
                }
            }
            return trends;
        }

        public static List<ApiGageReading> CalculatePredictions(Trends waterTrends,
                                                                Trends dischargeTrends,
                                                                List<SensorReading> latestReadings,
                                                                out double feetPerHour,
                                                                out double cfsPerHour)
        {
            feetPerHour = 0;
            cfsPerHour = 0;
            if (!waterTrends.TrendValue.HasValue)
            {
                return null;
            }

            feetPerHour = waterTrends.TrendValue.Value;
            cfsPerHour = dischargeTrends.TrendValue ?? 0;
            double feetPerPrediction = feetPerHour * ((double)Trends.PredictionIntervalMinutes / 60.0);
            double cfsPerPrediction = cfsPerHour * ((double)Trends.PredictionIntervalMinutes / 60.0);
            double curHeight = latestReadings[0].WaterHeightFeet.Value;
            double curDischarge = latestReadings[0].WaterDischarge ?? 0;
            DateTime curTime = latestReadings[0].Timestamp;
            List<ApiGageReading> ret = new List<ApiGageReading>();
            for (int i = 0; i < Trends.PredictionCount; i++)
            {
                curTime = curTime.AddMinutes(Trends.PredictionIntervalMinutes);
                curHeight += feetPerPrediction;
                curDischarge += cfsPerPrediction;
                ApiGageReading reading = new ApiGageReading()
                {
                    Timestamp = curTime,
                    WaterHeight = curHeight,
                    WaterDischarge = curDischarge,
                };
                ret.Add(reading);
            }
            return ret;
        }
    }
}
