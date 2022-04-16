SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SaveGageStatistics]
    @LocationId int,
    @Date datetime,
    @AverageBatteryMillivolts int = null,
    @PercentReadingsReceived float = null,
    @AverageRssi float = null,
    @SensorUpdateInterval int = null,
    @SensorUpdateIntervalChanged bit = null
AS
BEGIN

    IF EXISTS (SELECT * FROM GageStatistics WHERE LocationId=@LocationId AND Date=@Date)
    BEGIN
        UPDATE GageStatistics SET
            AverageBatteryMillivolts = @AverageBatteryMillivolts,
            PercentReadingsReceived = @PercentReadingsReceived,
            AverageRssi = @AverageRssi,
            SensorUpdateInterval = @SensorUpdateInterval,
            SensorUpdateIntervalChanged = @SensorUpdateIntervalChanged
        WHERE LocationId=@LocationId AND Date=@Date
    END
    ELSE
    BEGIN
        INSERT INTO GageStatistics
            (
            LocationId,
            Date,
            AverageBatteryMillivolts,
            PercentReadingsReceived,
            AverageRssi,
            SensorUpdateInterval,
            SensorUpdateIntervalChanged
            )
        VALUES 
            (
            @LocationId,
            @Date,
            @AverageBatteryMillivolts,
            @PercentReadingsReceived,
            @AverageRssi,
            @SensorUpdateInterval,
            @SensorUpdateIntervalChanged
            )
    END
END
GO
