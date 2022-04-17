using System.ComponentModel.DataAnnotations;

using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class FloodEventBase
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="Name is required."), StringLength(100,ErrorMessage ="Limit exceed")]
        public string EventName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        [Required(ErrorMessage ="Region is required")]
        public int RegionId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPublic { get; set; }

        public static FloodEventBase GetFloodEvent(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM FloodEvents WHERE Id = '{id}'", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "FloodEventBase.GetFloodEvent", ex);
            }
            return null;
        }

        public static async Task<List<FloodEventBase>> GetFloodEvents(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM FloodEvents", conn);
            List<FloodEventBase> ret = new List<FloodEventBase>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "FloodEventBase.GetFloodEvents", ex);
                return null;
            }
            return ret;
        }

        public static async Task MarkFloodEventsAsUndeleted(SqlConnection conn, IEnumerable<int> regionIds)
        {
            await SqlHelper.CallIdListProcedure(conn, "MarkFloodEventsAsUndeleted", regionIds, 180);
        }

        private static string GetColumnList()
        {
            return "Id, EventName, FromDate, ToDate, RegionId, IsActive, IsDeleted, IsPublic";
        }

        private static FloodEventBase InstantiateFromReader(SqlDataReader reader)
        {
            FloodEventBase flood = new FloodEventBase()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                EventName = SqlHelper.Read<string>(reader, "EventName"),
                FromDate = SqlHelper.Read<DateTime>(reader, "FromDate"),
                ToDate = SqlHelper.Read<DateTime>(reader, "ToDate"),
                RegionId = SqlHelper.Read<int>(reader, "RegionId"),
                IsActive = SqlHelper.Read<bool>(reader, "IsActive"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
                IsPublic = SqlHelper.Read<bool>(reader, "IsPublic"),
            };
            return flood;
        }
    }
}
