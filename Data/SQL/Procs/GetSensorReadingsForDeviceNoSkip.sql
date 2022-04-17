SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetSensorReadingsForDeviceNoSkip]
	@deviceId int,
	@readingCount int = null,
	@fromTime datetime = null,
	@toTime datetime = null
AS
BEGIN
	IF @readingCount IS NOT NULL
		SET ROWCOUNT @readingcount
	SELECT * from SensorReadings
		WHERE DeviceId = @deviceId
		AND IsDeleted = 0
		AND (@fromTime IS NULL OR Timestamp > @fromTime)
		AND (@toTime IS NULL OR Timestamp < @toTime)
		ORDER BY Timestamp DESC
	SET ROWCOUNT 0

END
GO
