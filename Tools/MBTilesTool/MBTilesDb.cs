using System.Security.Cryptography;

public class MBTilesDb
{
    private SqliteConnection conn;

    public MBTilesDb(SqliteConnection conn)
    {
        this.conn = conn;
    }

    // Region ID isn't used; it's just there to populate the objects...
    public async Task<List<MapGridEntry>> GetGridEntries(int regionId)
    {
        List<MapGridEntry> ret = [];
        using (SqliteCommand cmd = this.conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM map";
            using (SqliteDataReader dr = await cmd.ExecuteReaderAsync())
            {
                while (await dr.ReadAsync())
                {
                    MapGridEntry mge = new()
                    {
                        RegionId = regionId,
                        Zoom = SqlHelper.Read<int>(dr, "zoom_level"),
                        GridColumn = SqlHelper.Read<int>(dr, "tile_column"),
                        GridRow = SqlHelper.Read<int>(dr, "tile_row"),
                        TileId = SqlHelper.Read<string>(dr, "tile_id"),
                    };
                    ret.Add(mge);
                }
            }
        }
        return ret;
    }

    // Region ID isn't used; it's just there to populate the objects...
    public async Task<List<MapTileImage>> GetTileImages(int regionId)
    {
        List<MapTileImage> ret = [];
        using (MD5 md5 = MD5.Create())
        {
            using (SqliteCommand cmd = this.conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM images";
                using (SqliteDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        MapTileImage mti = new()
                        {
                            RegionId = regionId,
                            TileId = SqlHelper.Read<string>(dr, "tile_id"),
                            GzippedTileData = SqlHelper.Read<byte[]>(dr, "tile_data"),
                        };

                        // Note: currently, the tile source we're using uses the MD5 of the
                        // gzipped data as the tile ID, so this is currently redundant.  However,
                        // I don't want to rely on that, and it doesn't really hurt anything to
                        // recalculate the hash here, so...
                        byte[] hashedData = md5.ComputeHash(mti.GzippedTileData);
                        mti.Hash = Convert.ToHexString(hashedData);
                        ret.Add(mti);
                    }
                }
            }
        }
        return ret;
    }
}
