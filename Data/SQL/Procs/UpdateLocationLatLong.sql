SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [UpdateLocationLatLong]
    @LocationId int,
    @Latitude float,
    @Longitude float
AS
BEGIN

    UPDATE Locations SET Latitude=@Latitude, Longitude=@Longitude WHERE Id=@LocationId
END
