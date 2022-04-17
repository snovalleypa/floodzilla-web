SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SaveGageEvent]
    @Id int = null,
    @LocationId int,
    @EventType varchar(100),
    @EventTime datetime,
    @EventDetails text = null,
    @NotifierProcessedTime datetime = null,
    @NotificationResult varchar(100) = null
AS
BEGIN
    IF (isnull(@Id, 0) = 0)
    BEGIN
        INSERT INTO GageEvents(LocationId, EventType, EventTime, EventDetails, NotifierProcessedTime, NotificationResult)
        VALUES (@LocationId, @EventType, @EventTime, @EventDetails, @NotifierProcessedTime, @NotificationResult)
    END
    ELSE
    BEGIN
        UPDATE GageEvents SET
            LocationId = @LocationId,
            EventType = @EventType, 
            EventTime = @EventTime,
            EventDetails = @EventDetails,
            NotifierProcessedTime = @NotifierProcessedTime,
            NotificationResult = @NotificationResult
        WHERE
            Id = @Id
	END
END
GO
