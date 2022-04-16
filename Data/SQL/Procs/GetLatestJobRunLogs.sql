SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestJobRunLogs]
AS
BEGIN
SELECT jrl.* FROM JobRunLogs jrl JOIN
(
    SELECT jrl.JobName, MAX(jrl.StartTime) AS StartTime FROM JobRunLogs jrl
	    WHERE jrl.StartTime > DATEADD(DAY, -2, GETDATE())
    GROUP BY jrl.JobName
) latestRuns
on jrl.JobName = latestRuns.JobName and jrl.StartTime = latestRuns.StartTime
END
GO
