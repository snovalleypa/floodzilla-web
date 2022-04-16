SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SetLatestReceiver](
    @DeviceId int,
    @ExternalReceiverId varchar(70),
    @LastReadingReceived datetime = null
)
AS
BEGIN

    UPDATE Devices SET LatestReceiverId = @ExternalReceiverId, LastReadingReceived = @LastReadingReceived WHERE DeviceId = @DeviceId
	
END
GO
