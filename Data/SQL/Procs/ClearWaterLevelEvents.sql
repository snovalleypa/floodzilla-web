SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE ClearWaterLevelEvents
    @LocationId int,
    @StartDate datetime,
    @EndDate datetime
AS
BEGIN
    DELETE FROM WaterLevelEvents 
      WHERE LocationId = @LocationId AND Timestamp >= @StartDate AND Timestamp <= @EndDate
END
GO

