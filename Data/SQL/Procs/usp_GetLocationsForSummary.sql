/****** Object:  StoredProcedure [usp_GetLocationsForSummary]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_GetLocationsForSummary]
	@ChannelTypeId int = 1, --This default value is for Email
	@NotifyTypeId int = 1, --This default value is for Locations Summary
	@RegionId int = null,
	@OrganizationsID int = null,
	@DeviceTypeId int = 4 --This default value is for senix devices, excluding usgs and virtual devices
AS
BEGIN
	SET NOCOUNT ON;

	SELECT d.DeviceId, l.Id AS LocationId, l.LocationName, l.GroundHeight, l.Green, l.Brown, l.Yellow, u.FirstName, u.LastName, au.Email, u.UserId, @ChannelTypeId AS ChannelTypeId, @NotifyTypeId AS NotifyTypeId FROM Locations l
	INNER JOIN Devices d ON l.Id = d.LocationId
	INNER JOIN Regions r ON r.RegionId = l.RegionId
	INNER JOIN Organizations o ON o.OrganizationsID = r.OrganizationsID
	INNER JOIN (SELECT DISTINCT un.UserId, Users.AspNetUserId, Users.FirstName, Users.LastName, Users.OrganizationsID FROM Users INNER JOIN UserNotifications un ON Users.Id = un.UserId WHERE Users.IsDeleted <> 1 AND un.ChannelTypeId = @ChannelTypeId AND un.NotifyTypeId = @NotifyTypeId AND un.IsActive = 1) u ON u.OrganizationsID = o.OrganizationsID
	INNER JOIN AspNetUsers au ON au.Id = u.AspNetUserId
	WHERE l.IsActive = 1 AND (l.IsDeleted = 0 OR l.IsDeleted IS NULL) AND (r.IsDeleted = 0 OR r.IsDeleted IS NULL) AND (o.IsDeleted = 0 OR o.IsDeleted IS NULL) AND (d.IsDeleted = 0 OR d.IsDeleted IS NULL) AND (r.RegionId = @RegionId OR @RegionId IS NULL) AND (r.OrganizationsID = @OrganizationsID OR @OrganizationsID IS NULL) AND (d.DeviceTypeId = @DeviceTypeId OR @DeviceTypeId IS NULL)

	ORDER BY au.Email
END
GO
