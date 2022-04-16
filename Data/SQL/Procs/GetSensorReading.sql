SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetSensorReading]
	@id int,
	@readingCount int = null,
	@fromTime datetime = null,
	@toTime datetime = null,
	@skipCount int = 0,
	@lastReadingId int = 0
AS
BEGIN

	SELECT * from SensorReadings
		WHERE ID = @id
END
GO
