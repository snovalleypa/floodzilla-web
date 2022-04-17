SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SetSensorUpdateInterval](
    @DeviceId int,
    @SensorUpdateInterval int
)
AS
BEGIN

    UPDATE Devices SET SensorUpdateInterval = @SensorUpdateInterval WHERE DeviceId = @DeviceId
	
END
GO
