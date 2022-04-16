SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetGageStatisticsSummary]
AS
BEGIN

  SELECT * FROM GageStatistics GS1 JOIN
    (SELECT LocationId, MAX(Date) as MaxDate FROM GageStatistics GROUP BY LocationId) GS2
    ON GS1.LocationId = GS2.LocationId AND GS1.Date = GS2.MaxDate
  ORDER BY GS1.LocationId ASC
END
GO
