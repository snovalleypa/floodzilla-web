/****** Object:  StoredProcedure [usp_ValidateTempADCTestsCount]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_ValidateTempADCTestsCount]
AS
BEGIN
	declare @currutcdate as datetime
	set @currutcdate = getutcdate()

	UPDATE DevicesConfiguration SET DevicesConfiguration.ADCTestsCount = Regions.DefaultADCTestsCount
    
	FROM DevicesConfiguration 
    INNER JOIN Devices ON Devices.DeviceId = DevicesConfiguration.DeviceId 
    INNER JOIN Locations ON Locations.Id = Devices.LocationId 
    INNER JOIN Regions ON Regions.RegionId = Locations.RegionId 
    
	WHERE Regions.TempADCTestsCountValidTill <= @currutcdate AND DevicesConfiguration.ADCTestsCount = Regions.TempADCTestsCount AND NOT Regions.DefaultADCTestsCount IS NULL
    
	if (@@ROWCOUNT > 0)
	begin
		UPDATE Regions SET TempADCTestsCountValidTill = NULL, TempADCTestsCount = NULL WHERE TempADCTestsCountValidTill <= @currutcdate
	end
END
GO
