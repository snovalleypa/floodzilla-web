/****** Object:  StoredProcedure [usp_GetNotifyUsersInfo]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_GetNotifyUsersInfo]
	@ChannelTypeId int = 1, --This default value is for Email 
	@NotifyTypeId int = 1, --This default value is for Locations Summary
	@RegionId int = null,
	@OrganizationsID int = null,
	@DeviceTypeId int = 1 --This default value is for regular devices, excluding usgs and virtual devices
AS
BEGIN
	SET NOCOUNT ON;

	SELECT d.DeviceId, l.Id AS LocationId, l.LocationName, l.GroundHeight, l.Green, l.Brown, l.Yellow, u.FirstName, u.LastName, au.Email, u.CellPhone, u.DeviceToken, u.UserId, ln.NotifyId AS LocNotifyId, ln.Level1SentOn, ln.Level2SentOn, ln.Level3SentOn
	FROM Locations l
	INNER JOIN Devices d ON l.Id = d.LocationId
	INNER JOIN Regions r ON r.RegionId = l.RegionId
	INNER JOIN Organizations o ON o.OrganizationsID = r.OrganizationsID
	INNER JOIN (SELECT un.NotifyId, un.UserId, Users.AspNetUserId, Users.FirstName, Users.LastName, Users.OrganizationsID, Users.CellPhone, Users.DeviceToken FROM Users INNER JOIN UserNotifications un ON Users.Id = un.UserId WHERE (Users.IsDeleted = 0 OR Users.IsDeleted IS NULL) AND un.ChannelTypeId = @ChannelTypeId AND un.NotifyTypeId = @NotifyTypeId AND un.IsActive = 1) u ON u.OrganizationsID = o.OrganizationsID
	INNER JOIN AspNetUsers au ON au.Id = u.AspNetUserId

	LEFT OUTER JOIN LocNotifications ln ON (u.NotifyId = ln.NotifyId AND l.Id = ln.LocationId)

	WHERE l.IsActive = 1 AND (l.IsDeleted = 0 OR l.IsDeleted IS NULL) AND (r.IsDeleted = 0 OR r.IsDeleted IS NULL) AND (o.IsDeleted = 0 OR o.IsDeleted IS NULL) AND (d.IsDeleted = 0 OR d.IsDeleted IS NULL) AND (r.RegionId = @RegionId OR @RegionId IS NULL) AND (o.OrganizationsID = @OrganizationsID OR @OrganizationsID IS NULL) AND (d.DeviceTypeId = @DeviceTypeId OR @DeviceTypeId IS NULL)
END
GO
