/****** Object:  StoredProcedure [usp_SubscribeNotification]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_SubscribeNotification]
	@UserId int,
	@ChannelTypeId int,
	@NotifyTypeId int,
	@xml_LocIds varchar(max) = null
AS
BEGIN
	SET NOCOUNT ON;

	declare @notifyid int, @hdoc int

	INSERT INTO UserNotifications(UserId,	ChannelTypeId,	NotifyTypeId)
	VALUES						 (@UserId,	@ChannelTypeId,	@NotifyTypeId)

	SET @notifyid = SCOPE_IDENTITY()

	if (not @xml_LocIds is null)
	begin
		EXEC sp_xml_preparedocument @hdoc OUTPUT, @xml_LocIds

		INSERT INTO LocNotifications(UserId, ChannelTypeId,	 NotifyTypeId,	LocationId,	NotifyId)
		SELECT						@UserId, @ChannelTypeId, @NotifyTypeId, Id,			@notifyid
		FROM OPENXML (@hdoc, '/Locs/Loc', 1) WITH(Id INT) 
	
		EXEC sp_xml_removedocument @hdoc
	end
END
GO
