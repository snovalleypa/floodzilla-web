SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [SetPushNotificationLogResult]
    @Id int,
    @Result varchar(50),
    @Response text
AS
BEGIN
  UPDATE PushNotificationLog SET Result=@Result, Response=@Response WHERE Id=@id
END
GO
