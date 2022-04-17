/****** Object:  StoredProcedure [GetSensorReadingCountForDevice]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO





CREATE PROCEDURE [GetSensorReadingCountForDevice]
	@deviceId int,
	@fromTime datetime = null,
	@toTime datetime = null
AS
BEGIN
	SELECT COUNT(id) AS Count FROM SensorReadings
		WHERE DeviceId = @deviceId
		AND IsDeleted = 0
		AND (@fromTime IS NULL OR Timestamp > @fromTime)
		AND (@toTime IS NULL OR Timestamp < @toTime)

END
GO
