SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [GetActivePushNotificationAttempts]
  @ActiveTime datetime
AS
BEGIN
  SELECT * FROM PushNotificationAttempts
    WHERE NextActiveTime < @ActiveTime
END
GO

