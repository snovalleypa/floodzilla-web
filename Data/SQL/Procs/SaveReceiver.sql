/****** Object:  StoredProcedure [SaveReceiver]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SaveReceiver](
    @ReceiverId int,
    @ExternalReceiverId varchar(70),
    @Name varchar(200) = null,
    @Description nvarchar(200) = null,
    @Location nvarchar(200) = null,
    @ContactInfo nvarchar(200) = null,
    @ConnectionInfo nvarchar(200) = null,
    @SimId nvarchar(200) = null,
    @Latitude float = null,
    @Longitude float = null,
    @IsDeleted bit = 0)
AS
BEGIN

    UPDATE Receivers SET
         ExternalReceiverId = @ExternalReceiverId,
         Name = @Name,
         Description = @Description,
         Location = @Location,
         ContactInfo = @ContactInfo,
         ConnectionInfo = @ConnectionInfo,
         SimId = @SimId,
         Latitude = @Latitude,
         Longitude = @Longitude,
         IsDeleted = @IsDeleted
    where
         ReceiverId = @ReceiverId
	
END
GO
