SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [UpdatePushNotificationAttempt]
    @Id int,
    @Status int,
    @LastCheckTime datetime,
    @TicketId varchar(256) = null,
    @LastRetrySeconds int = null,
    @NextActiveTime datetime = null
AS
BEGIN
  UPDATE PushNotificationAttempts 
    SET Status = @Status, 
        LastCheckTime = @LastCheckTime, 
        TicketId = @TicketId, 
        LastRetrySeconds = @LastRetrySeconds,
        NextActiveTime = @NextActiveTime
    WHERE Id = @id
END
GO
