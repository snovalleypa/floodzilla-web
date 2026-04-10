SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Map_LoadTilesForPrecache]
    @RegionId int,
    @MaxTileZoomLevel int
AS
BEGIN
    SELECT * FROM Map_MapImages 
        WHERE TileId IN (
            SELECT TileId FROM Map_MapGrid
                WHERE RegionID = @RegionId
                AND (@MaxTileZoomLevel = 0 OR Zoom <= @MaxTileZoomLevel)
        )
END
GO
