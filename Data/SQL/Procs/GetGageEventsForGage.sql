SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [GetGageEventsForGage]
    @LocationId int,
    @MinDate datetime = null
AS
BEGIN
    SELECT * FROM GageEvents
        WHERE LocationId = @LocationId
        AND (@MinDate IS NULL OR EventTime > @MinDate)
END
GO
