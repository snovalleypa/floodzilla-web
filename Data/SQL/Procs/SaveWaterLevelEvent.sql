SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE SaveWaterLevelEvent
    @Id int = null,
    @LocationId int,
    @WaterHeight float = null,
    @WaterDischarge float = null,
    @EventType varchar(100),
    @Timestamp datetime,
    @Observed bit,
    @ApproximateTime bit,
    @Source varchar(100)
AS
BEGIN
    IF (isnull(@Id, 0) = 0)
    BEGIN
        INSERT INTO WaterLevelEvents(LocationId, WaterHeight, WaterDischarge, EventType, Timestamp, Observed, ApproximateTime, Source)
        VALUES(@LocationId, @WaterHeight, @WaterDischarge, @EventType, @Timestamp, @Observed, @ApproximateTime, @Source)
        SELECT @@IDENTITY
    END
    ELSE
    BEGIN
        UPDATE WaterLevelEvents SET
            LocationId = @LocationId,
            WaterHeight = @WaterHeight,
            WaterDischarge = @WaterDischarge,
            EventType = @EventType,
            Timestamp = @Timestamp,
            Observed = @Observed,
            ApproximateTime = @ApproximateTime,
            Source = @Source
        WHERE
            Id = @Id
        SELECT @Id
     END
END
GO

