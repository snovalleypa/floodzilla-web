SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetMinimalSensorReadingsForLocations]
	@idList varchar(200),
	@fromTime datetime,
	@toTime datetime
AS
BEGIN

	SELECT Id, Timestamp, LocationId, WaterHeightFeet, WaterDischarge from SensorReadings
		WHERE LocationId IN (SELECT value from string_split(@idList, ','))
		AND IsDeleted = 0
		AND Timestamp >= @fromTime
		AND Timestamp < @toTime
		ORDER BY LocationId ASC, Timestamp DESC
END
GO
