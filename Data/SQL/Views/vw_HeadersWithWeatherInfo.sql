/****** Object:  View [vw_HeadersWithWeatherInfo]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [vw_HeadersWithWeatherInfo]
AS
SELECT h.*, w.Temperature, w.Precip1HourMM, w.WeatherStatus FROM Headers h LEFT OUTER JOIN Weathers w ON h.WeatherId = w.WeatherId
GO
