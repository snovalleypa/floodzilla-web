
//$ TODO: Crest finding really needs to look at a wide window because of noise.
//$ For now we just record everything that looks like it could be a crest; we'll
//$ try to post-filter to find the "real" crest for a given event.

namespace FzCommon.Processors
{
    public class WaterLevelEventScanner
    {
        public WaterLevelEventScanner(Func<string, SensorReading, Task> foundEvent)
        {
            m_foundEvent = foundEvent;
        }
        private Func<string, SensorReading, Task> m_foundEvent;
        
        // If two consecutive readings are more than this many minutes apart, they
        // won't be used to try to compute crests
        const int CrestMaxTimeDeltaMinutes = 120;
        
        //$ TODO: Extractors so we can use something other than SensorReadings for these.
        //$ TODO: Handle discharge-only gages

        //$ TODO: Extractors for yellow/red/etc so we don't have to be tied to a SensorLocationBase?

        // Expects readings to be in oldest-first order.
        public async Task<bool> ProcessReadings(SensorLocationBase location, List<SensorReading> readings, SensorReading lastReading, bool lastReadingWasPossibleCrest)
        {
            // We're at the end of a set of readings; we can't really learn anything meaningful here.
            if (readings.Count < 2)
            {
                return false;
            }

            // If we happened to hit such that the crest was exactly on the last reading of the last batch,
            // we need to mark the crest.
            if (lastReadingWasPossibleCrest && (lastReading != null) && IsHighWater(location, lastReading))
            {
                if (readings[0].WaterHeightFeet <= lastReading.WaterHeightFeet)
                {
                    if (IsWithinCrestTimeRange(readings[0], lastReading))
                    {
                        await RecordEvent(WaterLevelEventTypes.Crest, lastReading);
                    }
                }
            }

            if (lastReading != null && readings.Count > 1)
            {
                await ProcessReadingSet(location, lastReading, readings[0], readings[1]);
            }
            for (int i = 1; i < readings.Count - 1; i++)
            {
                await ProcessReadingSet(location, readings[i - 1], readings[i], readings[i + 1]);
            }

            if (IsWithinCrestTimeRange(readings[readings.Count - 1], readings[readings.Count - 2]))
            {
                if (readings[readings.Count - 1].WaterHeightFeet > readings[readings.Count - 2].WaterHeightFeet)
                {
                    lastReadingWasPossibleCrest = true;
                }
            }

            return lastReadingWasPossibleCrest;
        }

        private async Task ProcessReadingSet(SensorLocationBase location, SensorReading before, SensorReading cur, SensorReading after)
        {
            if (!IsHighWater(location, cur))
            {
                if (IsHighWater(location, before))
                {
                    await RecordEvent(WaterLevelEventTypes.HighWaterEnd, cur);
                }
                return;
            }
            if (!IsHighWater(location, before))
            {
                await RecordEvent(WaterLevelEventTypes.HighWaterStart, cur);
            }

            if (cur.WaterHeightFeet > before.WaterHeightFeet && cur.WaterHeightFeet >= after.WaterHeightFeet)
            {
                if (IsWithinCrestTimeRange(cur, before) && IsWithinCrestTimeRange(after, cur))
                {
                    await RecordEvent(WaterLevelEventTypes.Crest, cur);
                }
            }

            await RecordThresholdEvent(before, cur, location.Brown, WaterLevelEventTypes.RedRising, WaterLevelEventTypes.RedFalling);
            await RecordThresholdEvent(before, cur, location.RoadSaddleHeight, WaterLevelEventTypes.RoadRising, WaterLevelEventTypes.RoadFalling);
        }

        private async Task RecordThresholdEvent(SensorReading before,
                                                SensorReading cur,
                                                double? threshold,
                                                string risingEvent,
                                                string fallingEvent)
        {
            if (!threshold.HasValue)
            {
                return;
            }

            bool beforeOver = (before.WaterHeightFeet >= threshold.Value);
            bool curOver = (cur.WaterHeightFeet >= threshold.Value);
            if (beforeOver != curOver)
            {
                if (curOver)
                {
                    await RecordEvent(risingEvent, cur);
                }
                else
                {
                    await RecordEvent(fallingEvent, cur);
                }
            }
        }

        private async Task RecordEvent(string eventType, SensorReading reading)
        {
            await m_foundEvent(eventType, reading);
        }

        //$ TODO: Does this need to be smarter than this?
        private bool IsHighWater(SensorLocationBase location, SensorReading reading)
        {
            return (location.Green.HasValue && location.Green.Value <= reading.WaterHeightFeet);
        }

        private bool IsWithinCrestTimeRange(SensorReading later, SensorReading earlier)
        {
            return (later.Timestamp.AddMinutes(-CrestMaxTimeDeltaMinutes) <= earlier.Timestamp);
        }
    }
}


