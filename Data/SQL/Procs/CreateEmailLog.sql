SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [CreateEmailLog]
    @Timestamp datetime,
	@MachineName varchar(50),
    @FromAddress varchar(100),
    @ToAddress varchar(100),
    @Subject nvarchar(200),
    @Text text,
    @TextIsHtml bit
AS
BEGIN
  INSERT INTO EmailLog(Timestamp, MachineName, FromAddress, ToAddress, Subject, Text, TextIsHtml)
    VALUES (@Timestamp, @MachineName, @FromAddress, @ToAddress, @Subject, @Text, @TextIsHtml)

  SELECT * FROM EmailLog WHERE Id=@@IDENTITY
END
GO
