/****** Object:  StoredProcedure [EnsureReceiver]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [EnsureReceiver]
(
@ExternalReceiverId varchar(70),
@LatestIPAddress varchar(70) = null
)
AS
BEGIN
    IF NOT EXISTS (SELECT ReceiverId FROM Receivers where ExternalReceiverId = @ExternalReceiverId AND IsDeleted = 0)
    BEGIN
        INSERT INTO Receivers (ExternalReceiverId) VALUES (@ExternalReceiverId)
    END

    IF NOT (@LatestIPAddress IS NULL)
    BEGIN
        UPDATE Receivers SET LatestIPAddress = @LatestIPAddress WHERE ExternalReceiverId = @ExternalReceiverId
    END

    -- This list must match the column list in ReceiverBase.cs
    SELECT ReceiverId, ExternalReceiverId, Name, Description, Location, ContactInfo, IsDeleted
    FROM Receivers
    WHERE ExternalReceiverId = @ExternalReceiverId
	
END
GO
