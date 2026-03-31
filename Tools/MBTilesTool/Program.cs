using System.Collections;
using System.Data;
using FzCommon;
using FzCommon.Map;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

//$ TODO: Metadata?

async Task CopyTileData(int regionId, MBTilesDb mb, SqlConnection to)
{
    List<(int id, string tileId, string hash)> existingTiles;
    List<MapGridEntry> existingGrids;
    List<MapTileImage> newTiles;
    List<MapGridEntry> newGrids;

    Console.WriteLine("Loading existing tiles...");
    existingTiles = await MapTileImage.GetAllTileImageHashesForRegion(to, regionId);
    Console.WriteLine("Loading existing grids...");
    existingGrids = await MapGridEntry.GetAllGridEntriesForRegion(to, regionId);
    Console.WriteLine("Loading new tiles...");
    newTiles = await mb.GetTileImages(regionId);
    Console.WriteLine("Loading new grids...");
    newGrids = await mb.GetGridEntries(regionId);

    Console.WriteLine("Replacing {0}/{1} old entries with {2}/{3} new entries", existingTiles.Count, existingGrids.Count, newTiles.Count, newGrids.Count);
    int skippedImageCount = 0;
    int addedImageCount = 0;
    int totalCount = 0;
    Console.Write("Ensuring images");
    foreach (MapTileImage newImage in newTiles)
    {
        totalCount++;
        if (totalCount % 500 == 0)
        {
            Console.Write(".");
        }
        bool foundMatch = false;
        for (int i = 0; i < existingTiles.Count; i++)
        {
            if (existingTiles[i].tileId == newImage.TileId)
            {
                foundMatch = (existingTiles[i].hash == newImage.Hash);
                existingTiles.RemoveAt(i);
                break;
            }
        }
        if (foundMatch)
        {
            skippedImageCount++;
        }
        else
        {
            addedImageCount++;
            await newImage.EnsureImage(to);
        }
    }
    Console.WriteLine("  {0} skipped, {1} added", skippedImageCount, addedImageCount);

    int skippedGridCount = 0;
    int addedGridCount = 0;
    totalCount = 0;
    Console.Write("Ensuring grid entries");
    foreach (MapGridEntry newEntry in newGrids)
    {
        totalCount++;
        if (totalCount % 500 == 0)
        {
            Console.Write(".");
        }

        bool foundMatch = false;
        for (int i = 0; i < existingGrids.Count; i++)
        {
            if (existingGrids[i].Zoom == newEntry.Zoom
                && existingGrids[i].GridColumn == newEntry.GridColumn
                && existingGrids[i].GridRow == newEntry.GridRow)
            {
                foundMatch = (existingGrids[i].TileId == newEntry.TileId);
                existingGrids.RemoveAt(i);
                break;
            }
        }
        if (foundMatch)
        {
            skippedGridCount++;
        }
        else
        {
            addedGridCount++;
            await newEntry.EnsureGridEntry(to);
        }
    }
    Console.WriteLine("  {0} skipped, {1} added", skippedGridCount, addedGridCount);

    Console.WriteLine("Had {0}/{1} items left over", existingTiles.Count, existingGrids.Count);

    foreach (MapGridEntry oldEntry in existingGrids)
    {
        await MapGridEntry.RemoveGridEntry(to, oldEntry.Id);
    }
    foreach ((int id, string tileId, string hash) oldImage in existingTiles)
    {
        await MapTileImage.RemoveImage(to, oldImage.id);
    }
}

if (args.Length < 2)
{
    Console.WriteLine("USAGE: MBTilesTool <mbtiles> <regionId>");
    Environment.Exit(-1);
}

int regionId = Int32.Parse(args[1]);

using (SqliteConnection tileConn = new($"Data Source={args[0]}"))
{
    await tileConn.OpenAsync();
    MBTilesDb mb = new(tileConn);
    FzConfig.Initialize();
    using (SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
    {
        await sqlcn.OpenAsync();
        await CopyTileData(regionId, mb, sqlcn);
        await sqlcn.CloseAsync();
    }
}
