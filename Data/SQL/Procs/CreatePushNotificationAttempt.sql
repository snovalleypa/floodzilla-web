SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [CreatePushNotificationAttempt]
    @Token varchar(4096),
    @Title NVARCHAR(128),
    @Subtitle NVARCHAR(128),
    @Body NVARCHAR(1024),
    @Data NVARCHAR(2048),
    @Status int,
    @SendTime datetime,
    @LastCheckTime datetime
AS
BEGIN
  INSERT INTO PushNotificationAttempts(Token, Title, Subtitle, Body, Data, Status, SendTime, LastCheckTime)
    VALUES (@Token, @Title, @Subtitle, @Body, @Data, @Status, @SendTime, @LastCheckTime)

  SELECT * FROM PushNotificationAttempts WHERE Id=@@IDENTITY
END
GO
