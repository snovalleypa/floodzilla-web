SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [CreatePushNotificationLog]
    @Timestamp datetime,
	@MachineName varchar(50),
    @LogEntryType varchar(50),
    @Tokens text,
    @Title NVARCHAR(128) = null,
    @Subtitle NVARCHAR(128) = null,
    @Body NVARCHAR(1024) = null,
    @Data NVARCHAR(2048) = null
AS
BEGIN
  INSERT INTO PushNotificationLog(Timestamp, MachineName, LogEntryType, Tokens, Title, Subtitle, Body, Data)
    VALUES (@Timestamp, @MachineName, @LogEntryType, @Tokens, @Title, @Subtitle, @Body, @Data)

  SELECT * FROM PushNotificationLog WHERE Id=@@IDENTITY
END
GO
