SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [CreatePushReceiptLog]
    @Timestamp datetime,
	@MachineName varchar(50),
    @LogEntryType varchar(50),
    @TicketIds text
AS
BEGIN
  INSERT INTO PushReceiptLog(Timestamp, MachineName, LogEntryType, TicketIds)
    VALUES (@Timestamp, @MachineName, @LogEntryType, @TicketIds)

  SELECT * FROM PushReceiptLog WHERE Id=@@IDENTITY
END
GO
