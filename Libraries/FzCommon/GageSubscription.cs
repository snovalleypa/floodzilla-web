using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    //$ TODO: save reading ID or something along with these?
    public class GageSubscription
    {
        public int UserId                       { get; set; }
        public int LocationId                   { get; set; }

        public static async Task<List<GageSubscription>> GetSubscriptionsForUser(SqlConnection sqlcn, int userId)
        {
            List<GageSubscription> ret = new List<GageSubscription>();
            using (SqlCommand cmd = new SqlCommand("GetUserGageSubscriptions", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

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

        public static async Task<List<GageSubscription>> GetSubscriptionsForGage(SqlConnection sqlcn, int locationId)
        {
            List<GageSubscription> ret = new List<GageSubscription>();
            using (SqlCommand cmd = new SqlCommand("GetSubscriptionsForGage", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@locationId", SqlDbType.Int).Value = locationId;

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

        public static async Task AddSubscription(SqlConnection sqlcn, int userId, int locationId)
        {
            using (SqlCommand cmd = new SqlCommand("AddUserGageSubscription", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task RemoveSubscription(SqlConnection sqlcn, int userId, int locationId)
        {
            using (SqlCommand cmd = new SqlCommand("RemoveUserGageSubscription", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static GageSubscription InstantiateFromReader(SqlDataReader reader)
        {
            return new GageSubscription()
            {
                UserId                  = SqlHelper.Read<int>(reader, "UserId"),
                LocationId              = SqlHelper.Read<int>(reader, "LocationId"),
            };
        }
    }
}

