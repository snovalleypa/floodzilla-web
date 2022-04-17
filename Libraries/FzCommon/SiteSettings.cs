using System.Data;

using Microsoft.Data.SqlClient;

// Sitewide settings -- even higher level than Region.
namespace FzCommon
{
    public class SiteSettings
    {
        public static SiteSettings GetSiteSettings()
        {
            if (s_siteSettings == null)
            {
                lock (s_lock)
                {
                    if (s_siteSettings == null)
                    {
                        s_siteSettings = LoadSettings();
                    }
                }
            }
            return s_siteSettings;
        }

        // If you have a SQL connection, use this version.
        public static SiteSettings GetSiteSettings(SqlConnection sqlcn)
        {
            if (s_siteSettings == null)
            {
                lock (s_lock)
                {
                    if (s_siteSettings == null)
                    {
                        s_siteSettings = LoadSettings(sqlcn);
                    }
                }
            }
            return s_siteSettings;
        }

        public int RegionId;
        public string SiteAdminEmail;
        public string SiteAdminSlackUrl;

        private static SiteSettings LoadSettings(SqlConnection sqlcn)
        {
            using (SqlCommand cmd = new SqlCommand("GetSiteSettings", sqlcn))
            {
                using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (dr.Read())
                    {
                        return new SiteSettings()
                        {
                            RegionId = SqlHelper.Read<int>(dr, "RegionId"),
                            SiteAdminEmail = SqlHelper.Read<string>(dr, "SiteAdminEmail"),
                            SiteAdminSlackUrl = SqlHelper.Read<string>(dr, "SiteAdminSlackUrl"),
                        };
                    }
                }
            }
            return null;
        }

        private static SiteSettings LoadSettings()
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                return LoadSettings(sqlcn);
            }
        }
        private static SiteSettings s_siteSettings = null;
        private static object s_lock = new object();

    }
}
