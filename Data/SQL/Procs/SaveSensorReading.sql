SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SaveSensorReading]
	@ListenerInfo varchar(200),
	@Timestamp datetime,
	@DeviceTimestamp datetime = null,
	@LocationId int,
	@DeviceId int,
    @DeviceTypeId int = null,
	@GroundHeight float = null,
	@DistanceReading float = null,
	@RawWaterHeight float = null,
	@WaterHeight float = null,
	@WaterDischarge float = null,
	@CalcWaterDischarge float = null,
	@BatteryVolt int = null,
	@RawSensorData text = null,
	@GroundHeightFeet float = null,
	@DistanceReadingFeet float = null,
	@RawWaterHeightFeet float = null,
	@WaterHeightFeet float = null,
    @IsDeleted bit = 0,
    @IsFiltered bit = 0,
    @BenchmarkElevation float = null,
    @RelativeSensorHeight float = null,
    @Green float = null,
    @Brown float = null,
    @RoadSaddleHeight float = null,
    @MarkerOneHeight float = null,
    @MarkerTwoHeight float = null,
    @Id int = null,
    @RSSI float = null,
    @SNR float = null,
    @DeleteReason nvarchar(200) = null
AS
BEGIN

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO SensorReadings
            (ListenerInfo, 
             Timestamp, 
             DeviceTimestamp, 
             LocationId, 
             DeviceId, 
             DeviceTypeId,
             GroundHeight, 
             DistanceReading, 
             RawWaterHeight,
             WaterHeight, 
             WaterDischarge, 
             CalcWaterDischarge, 
             BatteryVolt, 
             RawSensorData, 
             GroundHeightFeet, 
             DistanceReadingFeet, 
             RawWaterHeightFeet,
             WaterHeightFeet, 
             IsDeleted, 
             IsFiltered,
             BenchmarkElevation,
             RelativeSensorHeight,
             Green,
             Brown,
             RoadSaddleHeight,
             MarkerOneHeight,
             MarkerTwoHeight,
             RSSI,
             SNR,
             DeleteReason)
        VALUES
            (@ListenerInfo,
             @Timestamp, 
             @DeviceTimestamp,
             @LocationId,
             @DeviceId,
             @DeviceTypeId,
             @GroundHeight,
             @DistanceReading,
             @RawWaterHeight,
             @WaterHeight,
             @WaterDischarge,
             @CalcWaterDischarge,
             @BatteryVolt,
             @RawSensorData, 
             @GroundHeightFeet,
             @DistanceReadingFeet,
             @RawWaterHeightFeet,
             @WaterHeightFeet,
             @IsDeleted,
             @IsFiltered,
             @BenchmarkElevation,
             @RelativeSensorHeight,
             @Green,
             @Brown,
             @RoadSaddleHeight,
             @MarkerOneHeight,
             @MarkerTwoHeight,
             @RSSI,
             @SNR,
             @DeleteReason);
             
        SELECT @@IDENTITY AS Id
    END
    ELSE
    BEGIN
        UPDATE SensorReadings SET
            ListenerInfo = @ListenerInfo,
            Timestamp = @Timestamp, 
            DeviceTimestamp = @DeviceTimestamp,
            LocationId = @LocationId,
            DeviceId = @DeviceId,
            DeviceTypeId = @DeviceTypeId,
            GroundHeight = @GroundHeight,
            DistanceReading = @DistanceReading,
            RawWaterHeight = @RawWaterHeight,
            WaterHeight = @WaterHeight,
            WaterDischarge = @WaterDischarge,
            CalcWaterDischarge = @CalcWaterDischarge,
            BatteryVolt = @BatteryVolt,
            RawSensorData = @RawSensorData, 
            GroundHeightFeet = @GroundHeightFeet,
            DistanceReadingFeet = @DistanceReadingFeet,
            RawWaterHeightFeet = @RawWaterHeightFeet,
            WaterHeightFeet = @WaterHeightFeet,
            IsDeleted = @IsDeleted,
            IsFiltered = @IsFiltered,
            BenchmarkElevation = @BenchmarkElevation,
            RelativeSensorHeight = @RelativeSensorHeight,
            Green = @Green,
            Brown = @Brown,
            RoadSaddleHeight = @RoadSaddleHeight,
            MarkerOneHeight = @MarkerOneHeight,
            MarkerTwoHeight = @MarkerTwoHeight,
            RSSI = @RSSI,
            SNR = @SNR,
            DeleteReason = @DeleteReason
         WHERE Id = @Id

        SELECT @Id AS Id
    END
END
GO
