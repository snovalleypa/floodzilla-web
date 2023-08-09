using System.Data;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FzCommon
{
    public class GageEventTypes
    {
        public const string RedRising = "RedRising";
        public const string RedFalling = "RedFalling";
        public const string YellowRising = "YellowRising";
        public const string YellowFalling = "YellowFalling";
        public const string RoadRising = "RoadRising";
        public const string RoadFalling = "RoadFalling";
        public const string MarkedOffline = "MarkedOffline";
        public const string MarkedOnline = "MarkedOnline";
    }

    public class GageThresholdEventDetails
    {
        public double CurWaterLevel         { get; set; }
        public double PrevWaterLevel        { get; set; }
        public DateTime? RoadCrossing       { get; set; }
        public Trends Trends                { get; set; }
        public double? RoadSaddleHeight     { get; set; }
        public double? Yellow               { get; set; }
        public double? Red                  { get; set; }
        public ApiFloodLevel NewStatus      { get; set; }
    }

    //$ TODO: save reading ID or something along with these?
    public class GageEvent
    {
        public int Id                           { get; set; }
        public int LocationId                   { get; set; }
        public string EventType                 { get; set; }
        public DateTime EventTime               { get; set; }
        public string EventDetails              { get; set; }
        public DateTime? NotifierProcessedTime  { get; set; }
        public string NotificationResult        { get; set; }

        // Use a more liberal parser for these -- depending on how/where they got serialized,
        // the enum values may be ints instead of strings...
        private static JsonSerializerSettings DeserializeSettings = new JsonSerializerSettings()
        {
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter() { AllowIntegerValues = true },
            },
        };

        public GageThresholdEventDetails GageThresholdEventDetails
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<GageThresholdEventDetails>(this.EventDetails, DeserializeSettings);
                }
                catch
                {
                    // just eat this
                    return null;
                }
            }
            set
            {
                this.EventDetails = JsonConvert.SerializeObject(value);
            }
        }

        public static async Task<List<GageEvent>> GetEventsForGage(SqlConnection sqlcn, int locationId, DateTime? minDate)
        {
            List<GageEvent> ret = new List<GageEvent>();
            using (SqlCommand cmd = new SqlCommand("GetGageEventsForGage", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;
                if (minDate.HasValue) cmd.Parameters.Add("@minDate", SqlDbType.DateTime).Value = minDate;

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }

        public static async Task<List<GageEvent>> GetUnprocessedEvents(SqlConnection sqlcn)
        {
            List<GageEvent> ret = new List<GageEvent>();
            using (SqlCommand cmd = new SqlCommand("GetUnprocessedGageEvents", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(dr));
                    }
                }
            }
            return ret;
        }

        public static async Task<GageEvent> LoadAsync(SqlConnection sqlcn, int id)
        {
            using (SqlCommand cmd = new SqlCommand("GetGageEvent", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = id;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (await dr.ReadAsync())
                    {
                        return InstantiateFromReader(dr);
                    }
                }
            }
            return null;
        }

        public async Task Save(SqlConnection sqlcn)
        {
            using (SqlCommand cmd = new SqlCommand("SaveGageEvent", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = this.Id;
                cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = this.LocationId;
                cmd.Parameters.Add("@EventType", SqlDbType.VarChar, 100).Value = this.EventType;
                cmd.Parameters.Add("@EventTime", SqlDbType.DateTime).Value = this.EventTime;
                if (this.EventDetails != null)
                {
                    cmd.Parameters.Add("@EventDetails", SqlDbType.Text).Value = this.EventDetails;
                }
                else
                {
                    cmd.Parameters.Add("@EventDetails", SqlDbType.Text).Value = null;
                }
                if (this.NotifierProcessedTime.HasValue)
                {
                    cmd.Parameters.Add("@NotifierProcessedTime", SqlDbType.DateTime).Value = this.NotifierProcessedTime;
                }
                else
                {
                    cmd.Parameters.Add("@NotifierProcessedTime", SqlDbType.DateTime).Value = null;
                }
                if (!String.IsNullOrEmpty(this.NotificationResult))
                {
                    cmd.Parameters.Add("@NotificationResult", SqlDbType.VarChar, 100).Value = this.NotificationResult;
                }
                else
                {
                    cmd.Parameters.Add("@NotificationResult", SqlDbType.VarChar, 100).Value = null;
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static GageEvent InstantiateFromReader(SqlDataReader reader)
        {
            return new GageEvent()
            {
                Id                      = SqlHelper.Read<int>(reader, "Id"),
                LocationId              = SqlHelper.Read<int>(reader, "LocationId"),
                EventType               = SqlHelper.Read<string>(reader, "EventType"),
                EventTime               = SqlHelper.Read<DateTime>(reader, "EventTime"),
                EventDetails            = SqlHelper.Read<string>(reader, "EventDetails"),
                NotifierProcessedTime   = SqlHelper.Read<DateTime?>(reader, "NotifierProcessedTime"),
                NotificationResult      = SqlHelper.Read<string>(reader, "NotificationResult"),
            };
        }
    }
}

