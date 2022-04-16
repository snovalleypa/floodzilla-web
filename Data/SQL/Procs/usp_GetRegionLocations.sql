/****** Object:  StoredProcedure [usp_GetRegionLocations]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_GetRegionLocations]
	@RegionId int,
	@IsActive bit = null,
	@IsDeleted bit = null,
	@IsOffline bit = null
AS
BEGIN
	SET NOCOUNT ON;
	
	SELECT Locations.Id, Devices.DeviceTypeId, DevicesConfiguration.ADCTestsCount, Locations.Description, Locations.Address, Locations.IsActive, Locations.IsDeleted, Locations.IsOffline, 
	Locations.LocationName, Locations.TimeZone, Locations.Latitude, Locations.Longitude, Locations.Green, Locations.Brown, Locations.Yellow, Locations.GroundHeight, Locations.SeaLevel,
	Devices.Min, Devices.Max, Devices.MaxStDev, Locations.YMin, Locations.YMax FROM Locations
	Left JOIN Devices on Locations.Id=Devices.LocationId
	Left JOIN DevicesConfiguration on Devices.DeviceId=DevicesConfiguration.DeviceId
	WHERE (@IsActive IS NULL OR ISNULL(Locations.IsActive,0) = @IsActive) AND (@IsDeleted IS NULL OR ISNULL(Locations.IsDeleted,0) = @IsDeleted) AND (@IsOffline IS NULL OR ISNULL(Locations.IsOffline,0) = @IsOffline) AND Locations.RegionId = @RegionId
	
END
GO
