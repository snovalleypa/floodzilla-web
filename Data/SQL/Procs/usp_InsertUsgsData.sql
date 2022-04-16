/****** Object:  StoredProcedure [usp_InsertUsgsData]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_InsertUsgsData]
	@ObservedOn datetime,
	@SiteId int,
	@SiteName nvarchar(150) = null,
	@Latitude float = null,
	@Longitude float = null,
	@SteamFlow float = null,
	@GageHeight float = null
AS
BEGIN
	
	declare @id as int, @locid as int

	SELECT @locid = LocationId FROM Devices WHERE DeviceId = @SiteId
	UPDATE Locations SET Latitude = ISNULL(@Latitude,Latitude), Longitude = ISNULL(@Longitude,Longitude) WHERE Id = @locid
	
	UPDATE UsgsSites SET SiteName = ISNULL(@SiteName,SiteName), Latitude = ISNULL(@Latitude,Latitude), Longitude = ISNULL(@Longitude,Longitude) WHERE SiteId = @SiteId
	/*
	if (@@ROWCOUNT = 0)
	begin
		INSERT INTO UsgsSites(SiteId,  SiteName,  Latitude,  Longitude)
		VALUES				 (@SiteId, @SiteName, @Latitude, @Longitude)
	end
	
	UPDATE Devices SET Name = ISNULL(@SiteName,Name), Description = ISNULL(@SiteName,Description), @locid = LocationId WHERE DeviceId = @SiteId
	if (@@ROWCOUNT = 0)
	begin
		INSERT INTO Locations(LocationName, Description, TimeZone,				  RegionId, Latitude,  Longitude,  GroundHeight, IsActive, IsPublic, IsDeleted, IsOffline)
		VALUES				 (@SiteName,	@SiteName,	 'Pacific Standard Time', 1,		@Latitude, @Longitude, -1,			 1,		   1,		 0,			0		 )	
		set @locid = SCOPE_IDENTITY()

		INSERT INTO Devices(DeviceId, Name,		 Description, IsActive, IsDeleted, DeviceTypeId, LocationId)
		VALUES			   (@SiteId,  @SiteName, @SiteName,	  1,		0,		   2,			 @locid	   )
	end	
	*/

	if not exists (SELECT * FROM UsgsData WHERE ObservedOn = @ObservedOn AND SiteId = @SiteId)
	begin
		INSERT INTO UsgsData(ObservedOn,  SiteId,  SteamFlow,  GageHeight)
		VALUES				(@ObservedOn, @SiteId, @SteamFlow, @GageHeight)
		set @id = SCOPE_IDENTITY()

		INSERT INTO Headers(Timestamp,	 DeviceId, ModifiedOn,	LocationId,			UsgsDataId)
		VALUES			   (@ObservedOn, @SiteId,  @ObservedOn,	ISNULL(@locid,0),	@id		  )
		set @id = SCOPE_IDENTITY()

		INSERT INTO FzLevel(DeviceId, CreatedOn,   Iteration, HeaderId, LocationId,			IsDeleted, ModifiedOn)
		VALUES			   (@SiteId,  @ObservedOn, 6,		  @id,		ISNULL(@locid,0),	0,		   @ObservedOn)
	end
END
GO
