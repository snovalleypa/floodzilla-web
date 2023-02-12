SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestJobRunLogs]
AS
BEGIN
SELECT jrl.*,j.FriendlyName FROM 
  JobRunLogs jrl JOIN
  Jobs j
ON
  j.JobName = jrl.JobName AND j.LastStartTime = jrl.StartTime
END
GO
