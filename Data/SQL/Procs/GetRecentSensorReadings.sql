SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetRecentSensorReadings]
	@fromTime datetime
AS
BEGIN
    SELECT 
        Id, Timestamp, LocationId, WaterHeightFeet, WaterDischarge, BatteryVolt, IsDeleted, RoadSaddleHeight, GroundHeightFeet
    FROM SensorReadings
    WHERE Timestamp > @fromTime
    AND IsDeleted = 0
    ORDER BY LocationId ASC, Timestamp DESC
END
GO
