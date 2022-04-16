/****** Object:  StoredProcedure [usp_GetLocationsSummary]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_GetLocationsSummary]
	@ChannelTypeId int = 1, --This default value is for Email
	@NotifyTypeId int = 1, --This default value is for Locations Summary
	@Iteration int = 5,
	@RegionId int = null,
	@OrganizationsID int = null,
	@DeviceTypeId int = 1 --This default value is for regular devices, excluding usgs and virtual devices
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Locs.UserId, Locs.Email, Locs.FirstName, Locs.LastName, Locs.DeviceId, Locs.LocationId, Locs.LocationName, pings.CreatedOn AS LastUpdatedOn, pings.Height, pings.FloodLevel, pings.BatteryPercent, @ChannelTypeId AS ChannelTypeId, @NotifyTypeId AS NotifyTypeId FROM
	(
	SELECT d.DeviceId, l.Id AS LocationId, l.LocationName, l.GroundHeight, l.Green, l.Brown, l.Yellow, u.FirstName, u.LastName, au.Email, u.UserId FROM Locations l
	INNER JOIN Devices d ON l.Id = d.LocationId
	INNER JOIN Regions r ON r.RegionId = l.RegionId
	INNER JOIN Organizations o ON o.OrganizationsID = r.OrganizationsID
	INNER JOIN (SELECT DISTINCT un.UserId, Users.AspNetUserId, Users.FirstName, Users.LastName, Users.OrganizationsID FROM Users INNER JOIN UserNotifications un ON Users.Id = un.UserId WHERE Users.IsDeleted <> 1 AND un.ChannelTypeId = @ChannelTypeId AND un.NotifyTypeId = @NotifyTypeId AND un.IsActive = 1) u ON u.OrganizationsID = o.OrganizationsID
	INNER JOIN AspNetUsers au ON au.Id = u.AspNetUserId
	WHERE l.IsActive = 1 AND (l.IsDeleted = 0 OR l.IsDeleted IS NULL) AND (r.IsDeleted = 0 OR r.IsDeleted IS NULL) AND (o.IsDeleted = 0 OR o.IsDeleted IS NULL) AND (d.IsDeleted = 0 OR d.IsDeleted IS NULL) AND (r.RegionId = @RegionId OR @RegionId IS NULL) AND (r.OrganizationsID = @OrganizationsID OR @OrganizationsID IS NULL) AND (d.DeviceTypeId = @DeviceTypeId OR @DeviceTypeId IS NULL)
	) Locs
	OUTER APPLY 
	(
	SELECT TOP 1
	f.CreatedOn,
	(Locs.GroundHeight - dbo.udf_GetAverage(f.l1, f.l2, f.l3, f.l4, f.l5, f.l6, f.l7, f.l8, f.l9, f.l10, f.AvgL))/12.0 AS Height,
	dbo.udf_GetFloodLevel((Locs.GroundHeight - dbo.udf_GetAverage(f.l1, f.l2, f.l3, f.l4, f.l5, f.l6, f.l7, f.l8, f.l9, f.l10, f.AvgL))/12.0, Locs.Green, Locs.Brown, Locs.Yellow) AS FloodLevel,
	dbo.udf_GetBatteryStatus(h.BatteryVolt) AS BatteryPercent
	FROM FzLevel f INNER JOIN Headers h ON (f.HeaderId = h.Id)
	WHERE Locs.LocationId = f.LocationId AND f.Iteration = @Iteration ORDER BY f.Id DESC
	) pings

	ORDER BY Locs.Email
END
GO
