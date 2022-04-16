CREATE PROCEDURE GetUsersForNotifyForecastAlerts
AS
BEGIN
    SELECT * FROM Users WHERE NotifyForecastAlerts=1
END

