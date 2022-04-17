SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetRecentSensorReadingTimestampsByLocation]
    @regionId int,
    @fromDateTime dateTime,
    @ToDateTime dateTime
AS
BEGIN

SELECT sr.LocationId, MAX(sr.Timestamp) AS Timestamp FROM SensorReadings sr
    JOIN Locations l on l.Id = sr.LocationId
    WHERE 
      sr.Timestamp >= @fromDateTime AND 
      sr.Timestamp <= @ToDateTime AND 
      sr.IsDeleted = 0 AND 
      l.RegionId = @regionId
    GROUP BY sr.LocationId

END
GO
