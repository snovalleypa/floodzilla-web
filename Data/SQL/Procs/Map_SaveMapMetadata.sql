SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Map_SaveMapMetadata]
    @Id int,
    @RegionId int,
    @RefererList varchar(500),
    @SlugList varchar(500),
    @TileDescJson TEXT,
    @WebStyles TEXT,
    @MobileStyles TEXT
AS
BEGIN
    UPDATE Map_MapMetadata SET
        RegionId = @RegionId,
        RefererList = @RefererList,
        SlugList = @SlugList,
        TileDescJson = @TileDescJson,
        WebStyles = @WebStyles,
        MobileStyles = @MobileStyles
    WHERE
        Id = @id
END
GO
