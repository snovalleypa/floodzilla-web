CREATE PROCEDURE [GetJobRunLogsForName]
    @JobName nvarchar(50)
AS
BEGIN

    --//$ TODO: Date filtering? make row count a parameter?
    SET ROWCOUNT 100

    SELECT * FROM JobRunLogs
    WHERE JobName=@JobName
    ORDER BY StartTime DESC

    SET ROWCOUNT 0
END
GO
