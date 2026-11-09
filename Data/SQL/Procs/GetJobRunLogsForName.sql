CREATE PROCEDURE [GetJobRunLogsForName]
    @JobName nvarchar(50)
AS
BEGIN

    --//$ TODO: Date filtering? make row count a parameter?

    SELECT JRL.*, J.FriendlyName FROM JobRunLogs JRL
        LEFT JOIN Jobs J ON J.JobName = JRL.JobName
    WHERE JRL.JobName=@JobName
    ORDER BY StartTime DESC
    OFFSET 0 ROWS
    FETCH NEXT 100 ROWS ONLY

END
GO
