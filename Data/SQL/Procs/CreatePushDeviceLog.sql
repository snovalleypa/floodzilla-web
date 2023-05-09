SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [CreatePushDeviceLog]
    @Timestamp datetime,
	@MachineName varchar(50),
    @LogEntryType varchar(50),
    @Token varchar(4096) = null,
    @UserId int = null,
    @Platform varchar(64) = null,
    @Language varchar(64) = null,
    @Extra text = null
AS
BEGIN
  INSERT INTO PushDeviceLog(Timestamp, MachineName, LogEntryType, Token, UserId, Platform, Language, Extra)
    VALUES (@Timestamp, @MachineName, @LogEntryType, @Token, @UserId, @Platform, @Language, @Extra)

  SELECT * FROM PushDeviceLog WHERE Id=@@IDENTITY
END
GO
