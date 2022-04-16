SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestSensorReadingTimestamps]
	@fromTime datetime
AS
BEGIN
    SELECT LocationId, MAX(Timestamp) AS Timestamp FROM SensorReadings
    WHERE Timestamp > @fromTime
    AND IsDeleted = 0
    GROUP BY LocationId
END
GO
