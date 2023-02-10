CREATE PROCEDURE [GetAllJobsWithCurrentStatus]
AS
BEGIN
  SELECT 
    J.*,
    JL.Id as JobRunLogsId,
    JL.JobName as JobRunLogsJobName,
    JL.MachineName as JobRunLogsMachineName,
    JL.StartTime as JobRunLogsStartTime,
    JL.EndTime as JobRunLogsEndTime,
    JL.Summary as JobRunLogsSummary,
    JL.Exception as JobRunLogsException,
    JL.FullException as JobRunLogsFullException
  from Jobs J LEFT JOIN JobRunLogs JL on JL.Id = J.LastRunLogId
END
GO
