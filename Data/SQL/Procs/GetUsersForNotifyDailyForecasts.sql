CREATE PROCEDURE GetUsersForNotifyDailyForecasts
AS
BEGIN
    SELECT * FROM Users WHERE NotifyDailyForecasts=1
END

