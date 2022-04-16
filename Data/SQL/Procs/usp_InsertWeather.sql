/****** Object:  StoredProcedure [usp_InsertWeather]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_InsertWeather]
	@RegionId int,
	@WeatherStatus nvarchar(150),
	@Temperature float,
	@Precip1HourMM float,
	@ResponseString nvarchar(max)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO Weathers(RegionId,	WeatherStatus,  Temperature,  Precip1HourMM,  ResponseString)
	VALUES				(@RegionId,	@WeatherStatus,	@Temperature, @Precip1HourMM, @ResponseString)

	UPDATE Regions SET RecentWeatherId = SCOPE_IDENTITY() WHERE RegionId = @RegionId
END
GO
