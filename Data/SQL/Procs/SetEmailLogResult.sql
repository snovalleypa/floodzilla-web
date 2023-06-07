SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [SetEmailLogResult]
    @Id int,
    @Result varchar(50),
    @Details varchar(500) = null
AS
BEGIN
  UPDATE EmailLog SET Result=@Result, Details=@Details WHERE Id=@id
END
GO
