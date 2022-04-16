SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SaveNoaaForecastDataEntry]
    @ForecastId int,
    @Timestamp datetime,
    @Stage float = null,
    @Discharge float = null
AS
BEGIN
    INSERT INTO NoaaForecastData
        (ForecastId, Timestamp, Stage, Discharge)
    VALUES
        (@ForecastId, @Timestamp, @Stage, @Discharge)
END
GO
