SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [CreateSmsLog]
    @Timestamp datetime,
	@MachineName varchar(50),
    @LogEntryType varchar(50),
    @FromNumber varchar(100) = null,
    @ToNumber varchar(100) = null,
    @Text text = null
AS
BEGIN
  INSERT INTO SmsLog(Timestamp, MachineName, LogEntryType, FromNumber, ToNumber, Text)
    VALUES (@Timestamp, @MachineName, @LogEntryType, @FromNumber, @ToNumber, @Text)

  SELECT * FROM SmsLog WHERE Id=@@IDENTITY
END
GO
