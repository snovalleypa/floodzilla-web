CREATE PROCEDURE [GetSenixListenerLogsWithNoDevice]
	@fromTime datetime = null,
	@toTime datetime = null
AS
BEGIN

	SELECT * from SenixListenerLog
		WHERE DeviceId IS NULL
		AND (@fromTime IS NULL OR Timestamp > @fromTime)
		AND (@toTime IS NULL OR Timestamp < @toTime)
		ORDER BY Timestamp DESC
END
