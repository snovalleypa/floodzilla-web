SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetSensorReadingsNoSkip]
	@locationId int,
	@readingCount int = null,
	@fromTime datetime = null,
	@toTime datetime = null,
	@lastReadingId int = 0
AS
BEGIN

	IF @lastReadingId > 0
	BEGIN
		SELECT @fromTime = Timestamp FROM SensorReadings WHERE Id = @lastReadingId
	END

	IF @readingCount IS NOT NULL
		SET ROWCOUNT @readingcount
	SELECT * from SensorReadings
		WHERE LocationId = @locationId
		AND IsDeleted = 0
		AND (@fromTime IS NULL OR Timestamp > @fromTime)
		AND (@toTime IS NULL OR Timestamp < @toTime)
		ORDER BY Timestamp DESC
	SET ROWCOUNT 0
END
GO
