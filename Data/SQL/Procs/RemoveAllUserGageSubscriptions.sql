SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE RemoveAllUserGageSubscriptions
(
    @UserId int
)
AS
BEGIN
    DELETE FROM UserGageSubscriptions WHERE UserId=@UserId
END
GO
