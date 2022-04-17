SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestSensorReadingsByLocation]
    @regionId int
AS
BEGIN
SELECT sr.* FROM SensorReadings sr JOIN
(
    SELECT sr.LocationId, MAX(sr.Timestamp) AS Timestamp FROM SensorReadings sr
    JOIN Locations l on l.Id = sr.LocationId
    WHERE sr.IsDeleted = 0 AND l.RegionId = @regionId
    GROUP BY sr.LocationId
) latestReadings
on sr.LocationId = latestReadings.LocationId and sr.Timestamp = latestReadings.Timestamp
END
GO
