SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Map_GetAllTileImageHashesForRegion]
    @RegionId int
AS
BEGIN
    SELECT Id, TileId, Hash FROM Map_MapImages WHERE RegionId=@RegionId
END
GO
