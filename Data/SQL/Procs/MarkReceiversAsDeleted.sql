SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [MarkReceiversAsDeleted]
(
	@IdList varchar(200)
)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE Receivers SET IsDeleted=1 WHERE ReceiverId IN (SELECT value FROM string_split(@IdList,',')) 
END
GO
