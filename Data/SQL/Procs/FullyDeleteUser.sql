SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [FullyDeleteUser]
    @UserId int,
    @AspNetUserId varchar(100)
AS
BEGIN

    -- This is a bit of a mess.
    DELETE FROM DataSubscriptions WHERE UserId=@UserId
    DELETE FROM UserDevicePushTokens WHERE UserId=@UserId
    DELETE FROM UserGageSubscriptions WHERE UserId=@UserId
    DELETE FROM UserNotifications WHERE UserId=@UserId
    DELETE FROM Users WHERE Id=@UserId

    DELETE FROM AspNetUserClaims WHERE UserId=@AspNetUserId
    DELETE FROM AspNetUserLogins WHERE UserId=@AspNetUserId
    DELETE FROM AspNetUserRoles WHERE UserId=@AspNetUserId
    DELETE FROM AspNetUserTokens WHERE UserId=@AspNetUserId
    DELETE FROM AspNetUsers WHERE Id=@AspNetUserId
    

END

