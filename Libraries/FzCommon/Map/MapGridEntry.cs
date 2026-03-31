using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon.Map
{
    public class MapGridEntry
    {
        public int Id = -1;
        public int RegionId;
        public int Zoom;
        public int GridColumn;
        public int GridRow;
        public string TileId;

        public static async Task<List<MapGridEntry>> GetAllGridEntriesForRegion(SqlConnection sqlcn, int regionId)
        {
            List<MapGridEntry> ret = new();
            using (SqlCommand cmd = new SqlCommand("Map_GetAllGridEntriesForRegion", sqlcn))
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

        public async Task EnsureGridEntry(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("Map_EnsureGridEntry", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RegionId", this.RegionId);
            cmd.Parameters.AddWithValue("@Zoom", this.Zoom);
            cmd.Parameters.AddWithValue("@GridColumn", this.GridColumn);
            cmd.Parameters.AddWithValue("@GridRow", this.GridRow);
            cmd.Parameters.AddWithValue("@TileId", this.TileId);
            using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
            {
                if (!await dr.ReadAsync())
                {
                    throw new ApplicationException("Error ensuring map grid entry");
                }
                this.Id = SqlHelper.Read<int>(dr, "Id");
            }
        }

        public static async Task RemoveGridEntry(SqlConnection sqlcn, int id)
        {
            SqlCommand cmd = new SqlCommand("Map_RemoveGridEntry", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

       internal static MapGridEntry InstantiateFromReader(SqlDataReader dr)
        {
            return new MapGridEntry()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                RegionId = SqlHelper.Read<int>(dr, "RegionId"),
                Zoom = SqlHelper.Read<int>(dr, "Zoom"),
                GridColumn = SqlHelper.Read<int>(dr, "GridColumn"),
                GridRow = SqlHelper.Read<int>(dr, "GridRow"),
                TileId = SqlHelper.Read<string>(dr, "TileId"),
            };
        }
    }

}