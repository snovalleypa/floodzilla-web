SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetMinimalSensorReadings]
	@locationId int,
	@readingCount int = null,
	@fromTime datetime = null,
	@toTime datetime = null
AS
BEGIN

	SELECT Id, Timestamp, LocationId, WaterHeightFeet, WaterDischarge from SensorReadings
		WHERE LocationId = @locationId
		AND IsDeleted = 0
		AND (@fromTime IS NULL OR Timestamp > @fromTime)
		AND (@toTime IS NULL OR Timestamp < @toTime)
		ORDER BY Timestamp DESC
END
GO
