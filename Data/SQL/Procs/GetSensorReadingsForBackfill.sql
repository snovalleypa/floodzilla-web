SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [GetSensorReadingsForBackfill]
	@readingCount int
AS
BEGIN
    SET ROWCOUNT @readingcount
	SELECT * from SensorReadings
		WHERE DeviceTypeId IS NULL
	SET ROWCOUNT 0

END
GO
