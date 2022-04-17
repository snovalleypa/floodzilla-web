
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [GetNoaaForecastReadingsForSet]
(
	@IdList varchar(200)
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    SELECT * FROM NoaaForecastData WHERE ForecastId IN 
      (SELECT value FROM string_split(@IdList,','))
      ORDER BY ForecastId ASC, Timestamp ASC
END
GO
