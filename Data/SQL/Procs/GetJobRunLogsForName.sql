CREATE PROCEDURE [GetJobRunLogsForName]
    @JobName nvarchar(50)
AS
BEGIN

    --//$ TODO: Date filtering? make row count a parameter?
    SET ROWCOUNT 100

    SELECT JRL.*, J.FriendlyName FROM JobRunLogs JRL
    LEFT JOIN Jobs J ON J.JobName = JRL.JobName
    WHERE JRL.JobName=@JobName
    ORDER BY StartTime DESC

    SET ROWCOUNT 0
END
GO
