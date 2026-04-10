SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Map_EnsureGridEntry]
    @RegionId int,
    @Zoom int,
    @GridColumn int,
    @GridRow int,
	@TileId varchar(32)
AS
BEGIN

    IF EXISTS
        (SELECT TileId FROM Map_MapGrid WHERE RegionId=@RegionId AND Zoom=@Zoom AND GridColumn=@GridColumn AND GridRow=@GridRow)
    BEGIN
        UPDATE Map_MapGrid SET TileId=@TileId WHERE RegionId=@RegionId AND Zoom=@Zoom AND GridColumn=@GridColumn AND GridRow=@GridRow
    END
    ELSE
    BEGIN
        INSERT INTO Map_MapGrid (RegionId, Zoom, GridColumn, GridRow, TileId) VALUES (@RegionId, @Zoom, @GridColumn, @GridRow, @TileId) 
    END
    SELECT Id FROM Map_MapGrid WHERE RegionId=@RegionId AND Zoom=@Zoom AND GridColumn=@GridColumn AND GridRow=@GridRow
END
GO
