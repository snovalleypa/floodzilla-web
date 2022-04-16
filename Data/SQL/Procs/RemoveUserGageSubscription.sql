SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE RemoveUserGageSubscription
(
    @UserId int,
    @LocationId int
)
AS
BEGIN
    DELETE FROM UserGageSubscriptions WHERE UserId=@UserId AND LocationId=@LocationId
END
GO
