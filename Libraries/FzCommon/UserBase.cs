using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class UserBase
    {
        public int Id { get; set; }
        public string AspNetUserId { get; set; }
        [Required(ErrorMessage ="First name is required."),StringLength(50)]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last name is required."), StringLength(50)]
        public string LastName { get; set; }
        public string? Address { get; set; }
        public int? OrganizationsId { get; set; }
        public bool IsDeleted { get; set; }
        public bool NotifyViaEmail { get; set; }
        public bool NotifyViaSms { get; set; }
        public bool NotifyDailyForecasts { get; set; }
        public bool NotifyForecastAlerts { get; set; }
        public DateTime? CreatedOn { get; set; }

        public static UserBase GetUser(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Users WHERE Id = '{id}'", conn);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.GetUser", ex);
            }
            return null;
        }

        public static async Task<List<UserBase>> GetUsers(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Users", conn);
            List<UserBase> ret = new List<UserBase>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.GetUsers", ex);
            }
            return ret;
        }

        public static async Task<List<UserBase>> GetUsersForNotifyDailyForecasts(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand("GetUsersForNotifyDailyForecasts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            List<UserBase> ret = new List<UserBase>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.GetUsersForNotifyDailyForecasts", ex);
            }
            return ret;
        }

        public static async Task<List<UserBase>> GetUsersForNotifyForecastAlerts(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand("GetUsersForNotifyForecastAlerts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            List<UserBase> ret = new List<UserBase>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.GetUsersForNotifyForecastAlerts", ex);
            }
            return ret;
        }

        public static async Task MarkUsersAsUndeleted(SqlConnection conn, IEnumerable<int> regionIds)
        {
            await SqlHelper.CallIdListProcedure(conn, "MarkUsersAsUndeleted", regionIds, 180);
        }

        private static string GetColumnList()
        {
            return "Id, AspNetUserId, FirstName, LastName, Address, OrganizationsId, IsDeleted, NotifyViaEmail, NotifyViaSms, NotifyDailyForecasts, NotifyForecastAlerts, CreatedOn";
        }

        private static async Task<string> GetAspNetUserIdForPhone(SqlConnection sqlcn, string phone)
        {
            SqlCommand cmd = new SqlCommand($"SELECT Id FROM AspNetUsers WHERE PhoneNumber='{phone}'", sqlcn);
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return SqlHelper.Read<string>(reader, "Id");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.GetAspNetUserIdForPhone", ex);
            }
            return null;
        }

        public static async Task<bool> UnsubscribeSms(SqlConnection sqlcn, string phone)
        {
            string aspNetUserId = await GetAspNetUserIdForPhone(sqlcn, phone);
            if (String.IsNullOrEmpty(aspNetUserId))
            {
                // our SMS provider returns numbers with a country code, but we allow our users to omit it
                if (phone.StartsWith("1"))
                {
                    aspNetUserId = await GetAspNetUserIdForPhone(sqlcn, phone.Substring(1));
                }
            }
            if (String.IsNullOrEmpty(aspNetUserId))
            {
                return false;
            }

            SqlCommand cmd = new SqlCommand($"UPDATE Users SET NotifyViaSms=0 WHERE AspNetUserId='{aspNetUserId}'", sqlcn);
            try
            {
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.UnsubscribeSms", ex);
            }

            return false;
        }

        private static UserBase InstantiateFromReader(SqlDataReader reader)
        {
            UserBase user = new UserBase()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                AspNetUserId = SqlHelper.Read<string>(reader, "AspNetUserId"),
                FirstName = SqlHelper.Read<string>(reader, "FirstName"),
                LastName = SqlHelper.Read<string>(reader, "LastName"),
                Address = SqlHelper.Read<string>(reader, "Address"),
                OrganizationsId = SqlHelper.Read<int?>(reader, "OrganizationsId"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
                NotifyViaEmail = SqlHelper.Read<bool>(reader, "NotifyViaEmail"),
                NotifyViaSms = SqlHelper.Read<bool>(reader, "NotifyViaSms"),
                NotifyDailyForecasts = SqlHelper.Read<bool>(reader, "NotifyDailyForecasts"),
                NotifyForecastAlerts = SqlHelper.Read<bool>(reader, "NotifyForecastAlerts"),
                CreatedOn = SqlHelper.Read<DateTime?>(reader, "CreatedOn"),
            };
            return user;
        }
    }
}
