/****** Object:  StoredProcedure [usp_GetLocationsStatus]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_GetLocationsStatus]
	@RegionId int,
	@Iteration int = 5,
	@IsActive bit = null, @IsDeleted bit = null, @IsOffline bit = null

AS
BEGIN
	SET NOCOUNT ON;

	SELECT Locs.Id, Locs.Description, Locs.ADCTestsCount, Locs.DeviceTypeId, Locs.Address, Locs.IsActive, Locs.IsDeleted, Locs.IsOffline, Locs.LocationName, Locs.TimeZone, Locs.Latitude, Locs.Longitude, Locs.Green, 
	Locs.Brown, Locs.Yellow, Locs.GroundHeight, Locs.SeaLevel, Locs.Min, Locs.Max, Locs.MaxStDev, pings.WaterLevel, pings.BatteryPercent, pings.AvgL, pings.CreatedOn FROM
	(
	SELECT Locations.Id, Devices.DeviceTypeId, DevicesConfiguration.ADCTestsCount, Locations.Description, Locations.Address, Locations.IsActive, Locations.IsDeleted, Locations.IsOffline, 
	Locations.LocationName, Locations.TimeZone, Locations.Latitude, Locations.Longitude, Locations.Green, Locations.Brown, Locations.Yellow, Locations.GroundHeight, Locations.SeaLevel, Devices.Min, Devices.Max, Devices.MaxStDev FROM Locations
	Left JOIN Devices on Locations.Id=Devices.LocationId
	Left JOIN DevicesConfiguration on Devices.DeviceId=DevicesConfiguration.DeviceId
	WHERE (@IsActive IS NULL OR ISNULL(Locations.IsActive,0) = @IsActive) AND (@IsDeleted IS NULL OR ISNULL(Locations.IsDeleted,0) = '0') AND 
	(@IsOffline IS NULL OR ISNULL(Locations.IsOffline,0) = @IsOffline) AND Locations.RegionId = @RegionId
	) Locs
	OUTER APPLY 
	(
	SELECT TOP 1
	(Locs.GroundHeight - dbo.udf_GetAverage(f.l1, f.l2, f.l3, f.l4, f.l5, f.l6, f.l7, f.l8, f.l9, f.l10, f.AvgL))/12.0 AS WaterLevel,
	dbo.udf_GetBatteryStatus(h.BatteryVolt) AS BatteryPercent,
	AvgL,
	CreatedOn
	FROM FzLevel f INNER JOIN Headers h ON (f.HeaderId = h.Id)
	WHERE Locs.Id = f.LocationId AND ISNULL(f.IsDeleted,0) = 0 AND (f.Iteration = 5 OR f.Iteration = 6) ORDER BY f.Id DESC
	) pings
END
GO
