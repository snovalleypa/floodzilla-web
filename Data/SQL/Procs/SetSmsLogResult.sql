SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [SetSmsLogResult]
    @Id int,
    @Result varchar(50),
    @Details varchar(500) = null
AS
BEGIN
  UPDATE SmsLog SET Result=@Result, Details=@Details WHERE Id=@id
END
GO
