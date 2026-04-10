using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace FzCommon.Map
{
    public class MapTileImage
    {
        public int Id = -1;
        public int RegionId;
        public string TileId;
        public byte[] GzippedTileData;
        public string Hash;

        public static async Task<List<MapTileImage>> GetAllTileImagesForRegion(SqlConnection sqlcn, int regionId)
        {
            List<MapTileImage> ret = new();
            using (SqlCommand cmd = new SqlCommand("Map_GetAllTileImagesForRegion", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RegionId", SqlDbType.Int).Value = regionId;
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

        public static async Task<List<(int id, string tileId, string hash)>> GetAllTileImageHashesForRegion(SqlConnection sqlcn, int regionId)
        {
            List<(int id, string tileId, string hash)> ret = new();
            using (SqlCommand cmd = new("Map_GetAllTileImageHashesForRegion", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RegionId", SqlDbType.Int).Value = regionId;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (await dr.ReadAsync())
                    {
                        int id = SqlHelper.Read<int>(dr, "Id");
                        string tileId = SqlHelper.Read<string>(dr, "TileId");
                        string hash = SqlHelper.Read<string>(dr, "Hash");
                        ret.Add((id, tileId, hash));

                    }
                }
            }
            return ret;
        }

        public static async Task<List<MapTileImage>> LoadTilesForPrecache(SqlConnection sqlcn, int regionId, int maxZoom)
        {
            List<MapTileImage> ret = new();
            using (SqlCommand cmd = new("Map_LoadTilesForPrecache", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RegionId", SqlDbType.Int).Value = regionId;
                cmd.Parameters.Add("@MaxTileZoomLevel", SqlDbType.Int).Value = maxZoom;
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

        public static async Task<MapTileImage?> LoadGridTile(SqlConnection sqlcn, int regionId, int zoom, int column, int row)
        {
            using (SqlCommand cmd = new("Map_GetGridTile", sqlcn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RegionId", SqlDbType.Int).Value = regionId;
                cmd.Parameters.Add("@Zoom", SqlDbType.Int).Value = zoom;
                cmd.Parameters.Add("@GridColumn", SqlDbType.Int).Value = column;
                cmd.Parameters.Add("@GridRow", SqlDbType.Int).Value = row;
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

        public async Task EnsureImage(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("Map_EnsureMapImage", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RegionId", this.RegionId);
            cmd.Parameters.AddWithValue("@TileId", this.TileId);
            cmd.Parameters.AddWithValue("@TileData", this.GzippedTileData);
            cmd.Parameters.AddWithValue("@Hash", this.Hash);
            using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
            {
                if (!await dr.ReadAsync())
                {
                    throw new ApplicationException("Error ensuring map image");
                }
                this.Id = SqlHelper.Read<int>(dr, "Id");
            }
        }

        public static async Task RemoveImage(SqlConnection sqlcn, int id)
        {
            SqlCommand cmd = new SqlCommand("Map_RemoveMapImage", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        internal static MapTileImage InstantiateFromReader(SqlDataReader dr)
        {
            return new MapTileImage()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                RegionId = SqlHelper.Read<int>(dr, "RegionId"),
                TileId = SqlHelper.Read<string>(dr, "TileId"),
                GzippedTileData = SqlHelper.Read<byte[]>(dr, "TileData"),
                Hash = SqlHelper.Read<string>(dr, "Hash"),
            };
        }
    }
}