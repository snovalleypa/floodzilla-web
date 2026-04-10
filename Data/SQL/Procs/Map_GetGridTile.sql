SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Map_GetGridTile]
    @RegionId int,
    @Zoom int,
    @GridColumn int,
    @GridRow int
AS
BEGIN
    SELECT MI.Id, MI.RegionId, MI.TileId, TileData, Hash FROM Map_MapImages MI
        JOIN Map_MapGrid MG ON MG.TileId = MI.TileID and MG.RegionId = MI.RegionId
        WHERE MG.RegionId = @RegionId AND MG.Zoom = @Zoom AND MG.GridColumn = @GridColumn AND MG.GridRow = @GridRow
END
GO
