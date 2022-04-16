SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE GetUserGageSubscriptions
	@UserId int
AS
BEGIN
    SELECT * from UserGageSubscriptions WHERE UserId=@UserId
END
GO
