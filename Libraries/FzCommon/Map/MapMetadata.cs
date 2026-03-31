using System.Data;
using Microsoft.Data.SqlClient;
using FzCommon;
using System.Security.Cryptography;
using System.Text;

namespace FzCommon.Map
{
    public class MapMetadata
    {
        public MapMetadata()
        {
            this.Id = -1;
        }

        public int Id { get; set; }
        public int RegionId { get; set; }
        public string RefererList { get; set; }
        public string SlugList { get; set; }

        public string TileDescJson { get { return m_tileDescJson; } }
        public string TileDescJsonHash { get { return m_tileDescJsonHash; } }
        public string WebStyles { get { return m_webStyles; } }
        public string WebStylesHash { get { return m_webStylesHash; } }
        public string MobileStyles { get { return m_mobileStyles; } }
        public string MobileStylesHash { get { return m_mobileStylesHash; } }

        public string TileDescJsonRaw
        {
            get
            {
                return m_tileDescJsonRaw;
            }
            set
            {
                m_tileDescJsonRaw = value;
                m_tileDescJson = SubstituteUrls(value);
                byte[] hashedData = s_md5.ComputeHash(Encoding.UTF8.GetBytes(m_tileDescJson));
                m_tileDescJsonHash = Convert.ToHexString(hashedData);
            }
        }
        public string WebStylesRaw
        {
            get
            {
                return m_webStylesRaw;
            }
            set
            {
                m_webStylesRaw = value;
                m_webStyles = SubstituteUrls(value);
                byte[] hashedData = s_md5.ComputeHash(Encoding.UTF8.GetBytes(m_webStyles));
                m_webStylesHash = Convert.ToHexString(hashedData);
            }
        }
        public string MobileStylesRaw
        {
            get
            {
                return m_mobileStylesRaw;
            }
            set
            {
                m_mobileStylesRaw = value;
                m_mobileStyles = SubstituteUrls(value);
                byte[] hashedData = s_md5.ComputeHash(Encoding.UTF8.GetBytes(m_mobileStyles));
                m_mobileStylesHash = Convert.ToHexString(hashedData);
            }
        }

        private string m_tileDescJson;
        private string m_webStyles;
        private string m_mobileStyles;
        private string m_tileDescJsonHash;
        private string m_webStylesHash;
        private string m_mobileStylesHash;
        private string m_tileDescJsonRaw;
        private string m_webStylesRaw;
        private string m_mobileStylesRaw;

        const string MAP_URL_BASE_TAG = "{MapUrlBase}";
        const string SITE_URL_BASE_TAG = "{SiteUrlBase}";
        private string SubstituteUrls(string raw)
        {
            string mapBaseUrl = String.Format("{0}/{1}", FzConfig.Config[FzConfig.Keys.MapTileBaseUrl], this.RegionId);
            string siteUrl = FzConfig.Config[FzConfig.Keys.SiteBaseUrl];
            return raw.Replace(MAP_URL_BASE_TAG, mapBaseUrl).Replace(SITE_URL_BASE_TAG, siteUrl);
        }

        private static Dictionary<int, MapMetadata> s_metadata = new();
        private static MD5 s_md5 = MD5.Create();
        public async static Task<MapMetadata> Get(int regionId)
        {
            if (s_metadata.ContainsKey(regionId))
            {
                return s_metadata[regionId];
            }
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                string baseUrl = String.Format("{0}/{1}", FzConfig.Config[FzConfig.Keys.MapTileBaseUrl], regionId);
                using (SqlCommand cmd = new SqlCommand("Map_GetMapMetadataForRegion", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@regionId", SqlDbType.Int).Value = regionId;
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (!await dr.ReadAsync())
                        {
                            throw new ApplicationException("Unable to initialize map metadata");
                        }
                        MapMetadata md = new();
                        md.Id = SqlHelper.Read<int>(dr, "Id");
                        md.RegionId = regionId;
                        md.TileDescJsonRaw = SqlHelper.Read<string>(dr, "TileDescJson");
                        md.MobileStylesRaw = SqlHelper.Read<string>(dr, "MobileStyles");
                        md.WebStylesRaw = SqlHelper.Read<string>(dr, "WebStyles");
                        md.RefererList = SqlHelper.Read<string>(dr, "RefererList");
                        md.SlugList = SqlHelper.Read<string>(dr, "SlugList");
                        s_metadata[regionId] = md;
                        return md;
                    }
                }
            }
        }

        public static void DropMetadataFromCache(int regionId)
        {
            s_metadata.Remove(regionId);
        }

        public async Task Save(SqlConnection sqlcn)
        {
            if (this.Id == -1)
            {
                throw new ApplicationException("Can't create MapMetadata");
            }
            SqlCommand cmd = new SqlCommand("Map_SaveMapMetadata", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@RegionId", this.RegionId);
            cmd.Parameters.AddWithValue("@TileDescJson", this.TileDescJsonRaw);
            cmd.Parameters.AddWithValue("@MobileStyles", this.MobileStylesRaw);
            cmd.Parameters.AddWithValue("@WebStyles", this.WebStylesRaw);
            cmd.Parameters.AddWithValue("@RefererList", this.RefererList);
            cmd.Parameters.AddWithValue("@SlugList", this.SlugList);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
