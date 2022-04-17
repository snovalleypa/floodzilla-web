/****** Object:  StoredProcedure [GetJobRunLogs]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetJobRunLogs]
	@readingCount int = null
AS
BEGIN

	IF @readingCount IS NOT NULL
		SET ROWCOUNT @readingcount
	SELECT * from JobRunLogs
		ORDER BY StartTime DESC
	SET ROWCOUNT 0
END
GO
