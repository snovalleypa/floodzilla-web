/****** Object:  StoredProcedure [usp_GetLocationsForWebCache]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE proc [usp_GetLocationsForWebCache]
@RegionId int
as
begin 

select
	l.Id,
	l.LocationName,
	l.RegionId,
	l.Latitude,
	l.Longitude,
	l.IsOffline,
	l.IsPublic,
	l.NearPlaces,
	l.PublicLocationId,
	l.[Rank],
	l.Green,
	l.Brown,
	l.YMin, 
	l.YMax,
	d.DeviceTypeId,
	d.Version,
	dc.ADCTestsCount,
	dt.DeviceTypeName,
	dc.SendIterationCount,
	dc.SenseIterationMinutes,
	l.BenchmarkElevation,
	l.RelativeSensorHeight,
	l.GroundHeight,
	l.RoadSaddleHeight,
	l.RoadDisplayName

from locations l
  left join devices d on l.id=d.locationid
  left join DevicesConfiguration dc on d.DeviceId=dc.DeviceId
  left join DeviceTypes dt on d.DeviceTypeId=dt.DeviceTypeId
               
where l.IsDeleted='0'
  and l.IsActive='1'
  and l.regionId=@RegionId
end
GO
