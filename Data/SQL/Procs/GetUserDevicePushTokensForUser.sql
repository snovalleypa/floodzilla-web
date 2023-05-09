SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetUserDevicePushTokensForUser]
  @userId int
AS 
BEGIN
  SELECT * FROM UserDevicePushTokens WHERE UserId = @userId
END
GO

