SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [EnsureUserDevicePushToken]
    @UserId int,
    @Token varchar(4096),
    @Platform varchar(64),
    @Timestamp datetime,
    @Language varchar(64) = null,
    @DeviceId varchar(256) = null
AS
BEGIN
  IF EXISTS (SELECT * FROM UserDevicePushTokens WHERE UserId=@UserId AND Token=@Token)
  BEGIN
    UPDATE UserDevicePushTokens 
        SET LastRefreshTime=@Timestamp,
            Platform=@Platform,
            Language=@Language,
            DeviceId=@DeviceId
        WHERE UserID=@UserId AND Token=@Token
  END
  ELSE
  BEGIN
    INSERT INTO UserDevicePushTokens (UserId, Token, Platform, LastRefreshTime, Language, DeviceId)
        VALUES (@UserId, @Token, @Platform, @Timestamp, @Language, @DeviceId)
  END
END
GO
