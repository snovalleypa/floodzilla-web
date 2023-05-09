using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class AspNetUserBase
    {
        public string AspNetUserId          { get; set; }
        public string? Email                { get; set; }
        public bool EmailConfirmed          { get; set; }
        public string? PhoneNumber          { get; set; }
        public bool PhoneNumberConfirmed    { get; set; }

        public static AspNetUserBase GetAspNetUser(SqlConnection conn, string aspNetUserId)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM AspNetUsers WHERE Id = '{aspNetUserId}'", conn);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "AspNetUserBase.GetAspNetUser", ex);
            }
            return null;
        }

        public static async Task<AspNetUserBase> GetAspNetUserForEmailAsync(SqlConnection conn, string email)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM AspNetUsers WHERE Email = '{email}'", conn);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "AspNetUserBase.GetAspNetUserForEmailAsync", ex);
            }
            return null;
        }

        private static string GetColumnList()
        {
            return "Id, Email, EmailConfirmed, PhoneNumber, PhoneNumberConfirmed";
        }

        private static AspNetUserBase InstantiateFromReader(SqlDataReader reader)
        {
            AspNetUserBase user = new AspNetUserBase()
            {
                AspNetUserId = SqlHelper.Read<string>(reader, "Id"),
                Email = SqlHelper.Read<string>(reader, "Email"),
                EmailConfirmed = SqlHelper.Read<bool>(reader, "EmailConfirmed"),
                PhoneNumber = SqlHelper.Read<string>(reader, "PhoneNumber"),
                PhoneNumberConfirmed = SqlHelper.Read<bool>(reader, "PhoneNumberConfirmed"),
            };
            return user;
        }
    }
}
