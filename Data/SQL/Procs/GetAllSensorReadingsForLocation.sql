/****** Object:  StoredProcedure [GetAllSensorReadingsForLocation]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetAllSensorReadingsForLocation]
	@locationId int,
	@readingCount int = null,
	@fromTime datetime = null,
	@toTime datetime = null,
	@skipCount int = 0,
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
		AND (@fromTime IS NULL OR Timestamp > @fromTime)
		AND (@toTime IS NULL OR Timestamp < @toTime)
		ORDER BY Timestamp DESC
		OFFSET (@skipCount) ROWS
	SET ROWCOUNT 0
END
GO
