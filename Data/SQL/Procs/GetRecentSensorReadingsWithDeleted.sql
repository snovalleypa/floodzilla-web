SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetRecentSensorReadingsWithDeleted]
	@fromTime datetime
AS
BEGIN
    SELECT 
        Id, Timestamp, LocationId, WaterHeightFeet, WaterDischarge, BatteryVolt, IsDeleted, RoadSaddleHeight, GroundHeightFeet
    FROM SensorReadings
    WHERE Timestamp > @fromTime
    ORDER BY LocationId ASC, Timestamp DESC
END
GO
