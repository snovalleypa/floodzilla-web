/****** Object:  StoredProcedure [SaveJobRunLog]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [SaveJobRunLog]
	@JobName nvarchar(50),
	@MachineName varchar(50),
	@StartTime datetime,
	@EndTime datetime,
	@Summary nvarchar(500) = null,
	@Exception nvarchar(200) = null,
	@FullException text = null
AS
BEGIN

  IF NOT EXISTS (SELECT JobName FROM JobNames WHERE JobName=@JobName)
  BEGIN
    INSERT INTO JobNames (JobName) VALUES (@JobName)
  END

	INSERT INTO JobRunLogs (JobName, MachineName, StartTime, EndTime, Summary, Exception, FullException)
   	VALUES (@JobName, @MachineName, @StartTime, @EndTime, @Summary, @Exception, @FullException)
	END
GO
