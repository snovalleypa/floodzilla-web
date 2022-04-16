SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetWaterLevelEventsForLocation]
    @LocationId int
AS
BEGIN
    SELECT * FROM WaterLevelEvents
        WHERE LocationId = @LocationId
END
GO
