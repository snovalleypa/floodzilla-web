/****** Object:  StoredProcedure [SaveSenixListenerLog]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [SaveSenixListenerLog]
	@Timestamp datetime,
    @RawSensorData text,
    @Result nvarchar(200),
    @ListenerInfo varchar(200) = null,
    @ClientIP varchar(64) = null,
    @ExternalDeviceId varchar(70) = null,
    @DeviceId int = null,
    @ReadingId int = NULL,
    @Id int = null,
    @ReceiverId varchar(70) = null
AS
BEGIN

    IF (@id IS NULL OR @Id = 0)
    BEGIN
        INSERT INTO SenixListenerLog (Timestamp, ListenerInfo, ClientIP, RawSensorData, Result, ExternalDeviceId, ReceiverId, DeviceId, ReadingId) VALUES (@Timestamp, @ListenerInfo, @ClientIP, @RawSensorData, @Result, @ExternalDeviceId, @ReceiverId, @DeviceId, @ReadingId)
        SELECT @Id = @@IDENTITY
    END
    ELSE
    BEGIN
        UPDATE SenixListenerLog SET 
            Timestamp = @Timestamp,
            ListenerInfo = @ListenerInfo,
            ClientIP = @ClientIP,
            RawSensorData = @RawSensorData,
            Result = @Result,
            ExternalDeviceId = @ExternalDeviceId,
            ReceiverId = @ReceiverId,
            DeviceId = @DeviceId,
            ReadingId = @ReadingId
        WHERE ID=@Id
    END
    SELECT @Id As Id
END
