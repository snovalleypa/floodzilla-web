using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class RegionBase
    {
        [Required]
        public int RegionId { get; set; }

        [Required(ErrorMessage = "Organization is required.")]
        public int OrganizationsId { get; set; }

        [Required(ErrorMessage = "Name is required."), StringLength(150, ErrorMessage = "Region name must not exceed 150 characters.")]
        public string RegionName { get; set; }

        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? WindowsTimeZone { get; set; }
        public string? IanaTimeZone { get; set; }

        [Url(ErrorMessage = "Invalid URL.")]
        public string? BaseURL { get; set; }

        public string? NotifyList { get; set; }
        public int? SensorOfflineThreshold { get; set; }
        public string? SlackNotifyUrl { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPublic { get; set; }

        private TimeZoneInfo m_tzi = null;
        private TimeZoneInfo TimeZone
        {
            get
            {
                if (m_tzi == null)
                {
                    m_tzi = TimeZoneInfo.FindSystemTimeZoneById(this.WindowsTimeZone);
                }
                return m_tzi;
            }
        }

        public DateTime ToRegionTimeFromUtc(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, this.TimeZone);
        }

        public DateTime? ToRegionTimeFromUtc(DateTime? utc)
        {
            if (utc.HasValue)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utc.Value, this.TimeZone);
            }
            return null;
        }

        public DateTime ToUtcFromRegionTime(DateTime regionTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(regionTime, this.TimeZone);
        }

        public DateTime? ToUtcFromRegionTime(DateTime? regionTime)
        {
            if (regionTime.HasValue)
            {
                return TimeZoneInfo.ConvertTimeToUtc(regionTime.Value, this.TimeZone);
            }
            return null;
        }

        public static RegionBase GetRegion(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Regions WHERE RegionId = '{id}'", conn);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "RegionBase.GetRegion", ex);
            }
            return null;
        }

        public static async Task<RegionBase> GetRegionAsync(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Regions WHERE RegionId = '{id}'", conn);
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "RegionBase.GetRegion", ex);
            }
            return null;
        }

        public static async Task<List<RegionBase>> GetAllRegions(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Regions", conn);
            List<RegionBase> ret = new List<RegionBase>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "RegionBase.GetAllRegions", ex);
            }
            return ret;
        }

        public static async Task MarkRegionsAsUndeleted(SqlConnection conn, IEnumerable<int> regionIds)
        {
            await SqlHelper.CallIdListProcedure(conn, "MarkRegionsAsUndeleted", regionIds, 180);
        }

        private static string GetColumnList()
        {
            return "RegionId, OrganizationsId, RegionName, Address, Latitude, Longitude, WindowsTimeZone, IanaTimeZone, BaseURL, "
                  +"IsActive, IsDeleted, IsPublic, NotifyList, SensorOfflineThreshold, SlackNotifyUrl";
        }

        private static RegionBase InstantiateFromReader(SqlDataReader reader)
        {
            RegionBase region = new RegionBase()
            {
                RegionId = SqlHelper.Read<int>(reader, "RegionId"),
                OrganizationsId = SqlHelper.Read<int>(reader, "OrganizationsId"),
                RegionName = SqlHelper.Read<string>(reader, "RegionName"),
                Address = SqlHelper.Read<string>(reader, "Address"),
                Latitude = SqlHelper.Read<double?>(reader, "Latitude"),
                Longitude = SqlHelper.Read<double?>(reader, "Longitude"),
                WindowsTimeZone = SqlHelper.Read<string>(reader, "WindowsTimeZone"),
                IanaTimeZone = SqlHelper.Read<string>(reader, "IanaTimeZone"),
                BaseURL = SqlHelper.Read<string>(reader, "BaseURL"),
                IsActive = SqlHelper.Read<bool>(reader, "IsActive"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
                IsPublic = SqlHelper.Read<bool>(reader, "IsPublic"),
                NotifyList = SqlHelper.Read<string>(reader, "NotifyList"),
                SensorOfflineThreshold = SqlHelper.Read<int?>(reader, "SensorOfflineThreshold"),
                SlackNotifyUrl = SqlHelper.Read<string>(reader, "SlackNotifyUrl"),
            };
            return region;
        }
    }
}
