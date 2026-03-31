SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Map_EnsureMapImage]
    @RegionId int,
	@TileId varchar(32),
    @Hash varchar(32),
    @TileData varbinary(max)
AS
BEGIN

    IF EXISTS
        (SELECT TileData FROM Map_MapImages WHERE RegionId=@RegionId AND TileId=@TileId)
    BEGIN
        UPDATE Map_MapImages SET TileData=@TileData, Hash=@Hash WHERE RegionId=@RegionId AND TileId=@TileId
    END
    ELSE
    BEGIN
        INSERT INTO Map_MapImages (RegionId, TileId, Hash, TileData) VALUES (@RegionId, @TileId, @Hash, @TileData)
    END
    SELECT @TileId FROM Map_MapImages WHERE RegionId=@RegionId AND TileId=@TileId
END
GO
