using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class FloodEventLocationData
    {
        // Eventually, if we start storing data per event per location, it goes here.
      
        public int EventId { get; set; }
        public int LocationId { get; set; }
        
        public static async Task<List<FloodEventLocationData>> GetAllFloodEventLocationData(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM EventsDetail", conn);
            List<FloodEventLocationData> ret = new List<FloodEventLocationData>();
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "FloodEventLocationData.GetAllFloodEventLocationData", ex);
                return null;
            }
            return ret;
        }
        private static string GetColumnList()
        {
            return "EventId, LocationId";
        }

        private static FloodEventLocationData InstantiateFromReader(SqlDataReader reader)
        {
            FloodEventLocationData feld = new FloodEventLocationData()
            {
                EventId = SqlHelper.Read<int>(reader, "EventId"),
                LocationId = SqlHelper.Read<int>(reader, "LocationId"),
            };
            return feld;
        }
    }
}
