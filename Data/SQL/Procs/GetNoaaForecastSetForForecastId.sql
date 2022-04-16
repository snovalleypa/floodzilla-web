SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetNoaaForecastSetForForecastId]
    @ForecastId INT
AS
BEGIN

    DECLARE @Created DATETIME

    SELECT TOP 1 @Created = Created FROM NoaaForecasts
        WHERE ForecastId = @ForecastId
        ORDER BY Created Desc

    SELECT * FROM NoaaForecasts 
        WHERE Created = @Created
        ORDER BY ForecastId ASC

END
GO
