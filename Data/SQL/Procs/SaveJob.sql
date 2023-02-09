CREATE PROCEDURE [SaveJob]
(
    @Id int,
    @JobName nvarchar(50),
    @FriendlyName nvarchar(200),
    @IsEnabled bit,
    @LastStartTime DATETIME = NULL,
    @LastEndTime DATETIME = NULL,
    @LastSuccessfulEndTime DATETIME = NULL,
    @LastRunStatus NVARCHAR(50) = NULL,
    @LastRunLogId int = null,
    @LastRunSummary NVARCHAR(200) = NULL,
    @LastErrorTime DATETIME = NULL,
    @LastError NVARCHAR(200) = NULL,
    @LastFullException TEXT = NULL,
    @DisableReason NVARCHAR(200) = NULL,
    @DisabledTime DATETIME = NULL,
    @DisabledBy NVARCHAR(200) = NULL
)
AS
BEGIN
  UPDATE Jobs SET
    JobName = @JobName,
    FriendlyName = @FriendlyName,
    IsEnabled = @IsEnabled,
    LastStartTime = @LastStartTime,
    LastEndTime = @LastEndTime,
    LastSuccessfulEndTime = @LastSuccessfulEndTime,
    LastRunStatus = @LastRunStatus,
    LastRunLogId = @LastRunLogId,
    LastRunSummary = @LastRunSummary,
    LastErrorTime = @LastErrorTime,
    LastError = @LastError,
    LastFullException = @LastFullException,
    DisableReason = @DisableReason,
    DisabledTime = @DisabledTime,
    DisabledBy = @DisabledBy
  WHERE
    Id = @Id
END
GO
