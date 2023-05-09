using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class UserDevicePushToken
    {
        public int UserId;
        public string Token;
        public string Platform;
        public string? Language;
        public string? DeviceId;

        public UserDevicePushToken()
        {
            this.Token = "n/a";
        }

        //$ TODO: Normalize 'platform' if necessary
        public static async Task EnsureToken(SqlConnection sqlcn, int userId, string token, string platform, DateTime timestamp, string? language = null, string? deviceId = null)
        {
            using SqlCommand cmd = new SqlCommand("EnsureUserDevicePushToken", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Platform", platform);
            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            if (!String.IsNullOrEmpty(language))
            {
                cmd.Parameters.AddWithValue("@Language", language);
            }
            if (!String.IsNullOrEmpty(deviceId))
            {
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task RemoveToken(SqlConnection sqlcn, string token)
        {
            using SqlCommand cmd = new SqlCommand("RemoveUserDevicePushToken", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Token", token);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<UserDevicePushToken>> GetTokensForUser(SqlConnection sqlcn, int userId)
        {
            List<UserDevicePushToken> ret = new();
            using SqlCommand cmd = new SqlCommand("GetUserDevicePushTokensForUser", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "UserBase.GetUserForAspNetUserAsync", ex);
            }
            return ret;
        }

        private static UserDevicePushToken InstantiateFromReader(SqlDataReader reader)
        {
            return new UserDevicePushToken()
            {
                UserId = SqlHelper.Read<int>(reader, "UserId"),
                Token = SqlHelper.Read<string>(reader, "Token"),
                Platform = SqlHelper.Read<string>(reader, "Platform"),
                Language = SqlHelper.Read<string?>(reader, "Language"),
                DeviceId = SqlHelper.Read<string?>(reader, "DeviceId"),
            };
        }
    }
}
