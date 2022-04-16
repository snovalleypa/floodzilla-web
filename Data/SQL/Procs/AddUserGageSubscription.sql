SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE AddUserGageSubscription
(
    @UserId int,
    @LocationId int
)
AS
BEGIN
    IF NOT EXISTS (SELECT * FROM UserGageSubscriptions WHERE UserId=@UserId AND LocationId=@LocationID)
    BEGIN
        INSERT INTO UserGageSubscriptions (UserId, LocationId) VALUES (@UserId, @LocationId)
    END
END
GO
