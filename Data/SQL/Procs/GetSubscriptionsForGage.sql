SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE GetSubscriptionsForGage
	@LocationId int
AS
BEGIN
    SELECT * from UserGageSubscriptions WHERE LocationId=@LocationId
END
GO
x