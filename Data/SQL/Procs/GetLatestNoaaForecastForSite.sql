SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetLatestNoaaForecastForSite]
    @NoaaSiteId varchar(100)
AS
BEGIN

    DECLARE @ForecastId int

    SELECT TOP 1 @ForecastId=ForecastId FROM NoaaForecasts
        WHERE NoaaSiteId=@NoaaSiteId
        ORDER BY Created Desc

    SELECT * FROM NoaaForecasts WHERE ForecastId=@ForecastId

    SELECT * FROM NoaaForecastData WHERE ForecastId=@ForecastId ORDER BY Timestamp ASC

END
GO
