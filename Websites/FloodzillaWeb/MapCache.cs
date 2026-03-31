using FzCommon;
using FzCommon.Map;
using Microsoft.Data.SqlClient;

namespace FloodzillaWeb
{
    public class MapCache
    {
        private static MapCache s_cache = new MapCache();
        public static MapCache Get() { return MapCache.s_cache; }

        public MapCache()
        {
            m_grid = new();
            m_tiles = new();
        }

        public async Task PrecacheTiles(SqlConnection sqlcn, int regionId)
        {
            List<MapGridEntry> gridEntries = await MapGridEntry.GetAllGridEntriesForRegion(sqlcn, regionId);
            foreach (MapGridEntry gridEntry in gridEntries)
            {
                EnsureDicts(regionId, gridEntry.Zoom, gridEntry.GridRow, gridEntry.GridColumn);
                m_grid[regionId][gridEntry.Zoom][gridEntry.GridRow][gridEntry.GridColumn] = gridEntry.TileId;
            }
            int maxZoom = 0;
            if (FzConfig.Config[FzConfig.Keys.SuppressMapPrecache] == "true")
            {
                maxZoom = 8;
            }
            List<MapTileImage> tiles = await MapTileImage.LoadTilesForPrecache(sqlcn, regionId, maxZoom);
            foreach (MapTileImage tile in tiles)
            {
                m_tiles[tile.TileId] = (tile.GzippedTileData, tile.Hash);
            }
        }

        public (int gridCount, int tileCount, int totalSize) GetCacheStats(int regionId)
        {
            int gridCount = 0;
            int totalSize = 0;

            if (!m_grid.ContainsKey(regionId))
            {
                return (0, 0, 0);
            }
            foreach (int zoom in m_grid[regionId].Keys)
            {
                foreach (int gridRow in m_grid[regionId][zoom].Keys)
                {
                    foreach (int gridColumn in m_grid[regionId][zoom][gridRow].Keys)
                    {
                        string? tileId = m_grid[regionId][zoom][gridRow][gridColumn];
                        if (tileId != null)
                        {
                            gridCount++;
                        }
                    }
                }
            }
            foreach (var tile in m_tiles)
            {
                totalSize += tile.Value.gzippedTile.Length;
            }
            return (gridCount, m_tiles.Count, totalSize);
        }

        public string? GetCachedTileHash(int regionId, int zoom, int gridRow, int gridColumn)
        {
            EnsureDicts(regionId, zoom, gridRow, gridColumn);
            if (m_grid[regionId][zoom][gridRow].ContainsKey(gridColumn))
            {
                string? tileId = m_grid[regionId][zoom][gridRow][gridColumn];
                if (tileId != null)
                {
                    return m_tiles[tileId].hash;
                }
            }
            return null;
        }

        // Loads the grid/tile if it isn't already
        public async Task<(byte[]? gzippedTile, string? hash)> GetTileForGrid(SqlConnection sqlcn, int regionId, int zoom, int gridRow, int gridColumn)
        {
            await EnsureEntry(sqlcn, regionId, zoom, gridRow, gridColumn);
            string? tileId = m_grid[regionId][zoom][gridRow][gridColumn];
            if (tileId != null)
            {
                return m_tiles[tileId];
            }
            return (null, null);
        }

        private void EnsureDicts(int regionId, int zoom, int gridRow, int gridColumn)
        {
            if (!m_grid.ContainsKey(regionId))
            {
                m_grid[regionId] = new();
            }
            if (!m_grid[regionId].ContainsKey(zoom))
            {
                m_grid[regionId][zoom] = new();
            }
            if (!m_grid[regionId][zoom].ContainsKey(gridRow))
            {
                m_grid[regionId][zoom][gridRow] = new();
            }
        }

        private async Task EnsureEntry(SqlConnection sqlcn, int regionId, int zoom, int gridRow, int gridColumn)
        {
            EnsureDicts(regionId, zoom, gridRow, gridColumn);
            if (!m_grid[regionId][zoom][gridRow].ContainsKey(gridColumn))
            {
                MapTileImage? tile = await MapTileImage.LoadGridTile(sqlcn, regionId, zoom, gridColumn, gridRow);
                if (tile == null)
                {
                    m_grid[regionId][zoom][gridRow][gridColumn] = null;
                }
                else
                {
                    m_grid[regionId][zoom][gridRow][gridColumn] = tile.TileId;
                    m_tiles[tile.TileId] = (tile.GzippedTileData, tile.Hash);
                }
            }
        }

        private Dictionary<string, (byte[] gzippedTile, string hash)> m_tiles;
        private Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, string?>>>> m_grid;
    }
}