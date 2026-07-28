SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetRecentJobRunLogsWithDetails]
	@startTime DateTime
AS
BEGIN

    SELECT * from JobRunLogs
        WHERE StartTime >= @startTime
        AND Details IS NOT NULL
        ORDER BY StartTime DESC
END
GO
