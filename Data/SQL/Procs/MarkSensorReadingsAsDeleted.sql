/****** Object:  StoredProcedure [MarkSensorReadingsAsDeleted]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [MarkSensorReadingsAsDeleted]
(
	@IdList varchar(200),
    @Reason nvarchar(200) = null
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    -- Insert statements for procedure here
    UPDATE SensorReadings SET IsDeleted=1,DeleteReason=@Reason WHERE Id IN (SELECT value FROM string_split(@IdList,',')) 
END
GO
