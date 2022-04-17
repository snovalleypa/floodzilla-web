/****** Object:  StoredProcedure [usp_GetRealFloodData]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [usp_GetRealFloodData]
	@from datetime,	@to datetime,
	@RegionId int,
	@LocationId int = null,
	@Iteration int = 5,
	@IsActive bit = null
AS
BEGIN
	SET NOCOUNT ON;

	SELECT h.Timestamp, h.LocationId, l.Latitude, l.Longitude, (l.GroundHeight - dbo.udf_GetAverage(f.l1, f.l2,f.l3,f.l4,f.l5,f.l6,f.l7,f.l8,f.l9,f.l10,null)) AS ActualGHeight, ISNULL(w.Precip1HourMM, 0) AS ActualRain
	FROM (SELECT HeaderId, l1, l2, l3, l4, l5, l6, l7, l8, l9, l10 FROM FzLevel WHERE Iteration = @Iteration) f
	INNER JOIN Headers h ON (f.HeaderId = h.Id)
	INNER JOIN (SELECT Id, RegionId, Latitude, Longitude, GroundHeight FROM Locations WHERE RegionId = @RegionId AND (Id = @LocationId OR @LocationId IS NULL) AND (IsActive = @IsActive OR @IsActive IS NULL)) l ON (h.LocationId = l.Id)
	LEFT OUTER JOIN (SELECT DATEPART(YEAR, Timestamp) AS WYear, DATEPART(MONTH, Timestamp) AS WMonth, DATEPART(DAY, Timestamp) AS WDay, DATEPART(HOUR, Timestamp) AS WHour, MAX(Precip1HourMM) AS Precip1HourMM FROM Weathers WHERE Timestamp >= @from AND Timestamp <= @to AND RegionId = @RegionId GROUP BY DATEPART(YEAR, Timestamp), DATEPART(MONTH, Timestamp), DATEPART(DAY, Timestamp), DATEPART(HOUR, Timestamp)) w
	ON (DATEPART(YEAR, h.Timestamp) = WYear AND DATEPART(MONTH, h.Timestamp) = WMonth AND DATEPART(DAY, h.Timestamp) = WDay AND DATEPART(HOUR, h.Timestamp) = WHour)
	WHERE h.Timestamp >= @from AND h.Timestamp <= @to
	ORDER BY h.Timestamp ASC
END
GO
