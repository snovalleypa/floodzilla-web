using System.Data;
using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;

// There are some differences between this and GageEvent: GageEvent is primarily
// geared toward events tied to notifications. WaterLevelEvent is about capturing
// any significant event that happens at a gage; it's designed for viewing historical
// data, not notification or reporting.

//$ TODO: Do we really need both?
//$ TODO: Even if we really need both, it's probably just one piece of code that
//$   updates both for realtime events

namespace FzCommon
{
    public class WaterLevelEventTypes
    {
        public const string RedRising = "RedRising";
        public const string RedFalling = "RedFalling";
        public const string RoadRising = "RoadRising";
        public const string RoadFalling = "RoadFalling";
        public const string Crest = "Crest";
        public const string HighWaterStart = "HighWaterStart";
        public const string HighWaterEnd = "HighWaterEnd";
    }

    public class WaterLevelEvent
    {
        public int Id                           { get; set; }
        public int LocationId                   { get; set; }
        public string EventType                 { get; set; }
        public DateTime Timestamp               { get; set; }
        public double? WaterHeight              { get; set; }
        public double? WaterDischarge           { get; set; }
        public bool Observed                    { get; set; }
        public bool ApproximateTime             { get; set; }
        public string Source                    { get; set; }

        public WaterLevelEvent()
        {
            this.Id = 0;
        }

        public WaterLevelEvent(SensorReading reading)
        {
            this.Id = 0;
            this.Timestamp = reading.Timestamp;
            this.WaterHeight = reading.WaterHeightFeet;
            this.WaterDischarge = reading.WaterDischarge;
            this.Observed = true;
            this.ApproximateTime = false;
        }

        //$ TODO: parameters
        public static async Task<List<WaterLevelEvent>> GetWaterLevelEvents(SqlConnection sqlcn, int locationId)
        {
            List<WaterLevelEvent> ret = new List<WaterLevelEvent>();
            using (SqlCommand cmd = new SqlCommand("GetWaterLevelEventsForLocation", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }

        public static async Task ClearWaterLevelEvents(SqlConnection sqlcn, int locationId, DateTime? startDateUtc, DateTime? endDateUtc)
        {
            using (SqlCommand cmd = new SqlCommand("ClearWaterLevelEvents", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
                cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDateUtc.HasValue ? startDateUtc.Value : SqlDateTime.MinValue;
                cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDateUtc.HasValue? endDateUtc.Value : SqlDateTime.MaxValue;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task Save(SqlConnection sqlcn)
        {
            using (SqlCommand cmd = new SqlCommand("SaveWaterLevelEvent", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = this.LocationId;
                cmd.Parameters.Add("@EventType", SqlDbType.VarChar, 100).Value = this.EventType;
                cmd.Parameters.Add("@Timestamp", SqlDbType.DateTime).Value = this.Timestamp;
                if (this.WaterHeight.HasValue)
                {
                    cmd.Parameters.Add("@WaterHeight", SqlDbType.Float).Value = this.WaterHeight.Value;
                }
                else
                {
                    cmd.Parameters.Add("@WaterHeight", SqlDbType.Float).Value = null;
                }
                if (this.WaterDischarge.HasValue)
                {
                    cmd.Parameters.Add("@WaterDischarge", SqlDbType.Float).Value = this.WaterDischarge.Value;
                }
                else
                {
                    cmd.Parameters.Add("@WaterDischarge", SqlDbType.Float).Value = null;
                }
                cmd.Parameters.Add("@Observed", SqlDbType.Bit).Value = this.Observed;
                cmd.Parameters.Add("@ApproximateTime", SqlDbType.Bit).Value = this.ApproximateTime;
                cmd.Parameters.Add("@Source", SqlDbType.VarChar, 100).Value = this.Source;

                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    if (!await dr.ReadAsync())
                    {
                        throw new ApplicationException("Error saving WaterLevelEvent");
                    }
                    this.Id = SqlHelper.Read<int>(dr, "Id");
                }
            }
        }

        private static WaterLevelEvent InstantiateFromReader(SqlDataReader reader)
        {
            return new WaterLevelEvent()
            {
                Id                      = SqlHelper.Read<int>(reader, "Id"),
                LocationId              = SqlHelper.Read<int>(reader, "LocationId"),
                EventType               = SqlHelper.Read<string>(reader, "EventType"),
                Timestamp               = SqlHelper.Read<DateTime>(reader, "Timestamp"),
                WaterHeight             = SqlHelper.Read<double?>(reader, "WaterHeight"),
                WaterDischarge          = SqlHelper.Read<double?>(reader, "WaterDischarge"),
                Observed                = SqlHelper.Read<bool>(reader, "Observed"),
                ApproximateTime         = SqlHelper.Read<bool>(reader, "ApproximateTime"),
                Source                  = SqlHelper.Read<string>(reader, "Source"),
            };
        }
    }
}

