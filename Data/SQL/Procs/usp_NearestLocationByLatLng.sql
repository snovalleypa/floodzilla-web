/****** Object:  StoredProcedure [usp_NearestLocationByLatLng]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE Proc [usp_NearestLocationByLatLng]
@lat float,
@lng float
as
begin

declare @point as geography
set @point=geography::Point(@lat,@lng,4326)

-- STDistance gives distance in meters by converting it into KM so that the location within one KM can also be returned

SELECT top 10 Id,LocationName,Latitude,Longitude, GeoData.STDistance(@point)/1000 as DistanceKm FROM locations  
WHERE GeoData.STDistance(@point) IS NOT NULL  and (GeoData.STDistance(@point)/1000) <=1
ORDER BY DistanceKm 

end
GO
