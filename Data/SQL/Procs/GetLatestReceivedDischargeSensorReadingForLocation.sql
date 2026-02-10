SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestReceivedDischargeSensorReadingForLocation]
    @locationId int
AS
BEGIN
    SELECT TOP 1 * FROM SensorReadings
        WHERE LocationId = @locationId
        AND WaterDischarge IS NOT NULL
    ORDER BY Timestamp DESC
END
GO

