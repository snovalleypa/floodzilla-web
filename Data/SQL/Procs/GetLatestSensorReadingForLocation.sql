SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestSensorReadingForLocation]
	@locationId int
AS
BEGIN

	SELECT top 1 * from SensorReadings
		WHERE LocationId = @locationId
        AND IsDeleted = 0
		ORDER BY Timestamp DESC
END
GO
