SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetGageEvent]
    @EventId int
AS
BEGIN
    SELECT * FROM GageEvents
        WHERE Id = @EventId
END
GO
