SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetLatestNoaaForecastSet]
AS
BEGIN

  SELECT * FROM NoaaForecasts 
    WHERE ForecastId IN (
      SELECT MAX(ForecastId) FROM NoaaForecasts GROUP BY NoaaSiteId
    )
    ORDER BY ForecastId ASC

END
GO
