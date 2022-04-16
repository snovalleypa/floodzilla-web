SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [SaveNoaaForecast]
    @ForecastId int = null,
    @NoaaSiteId varchar(100),
    @Created datetime,
    @Description nvarchar(200) = null,
    @County nvarchar(100) = null,
    @State nvarchar(100) = null,
    @Latitude float = null,
    @Longitude float = null,
    @Elevation float = null,
    @BankFullStage float = null,
    @FloodStage float = null,
    @CurrentWaterHeight float = null,
    @CurrentDischarge float = null,
    @PurgeData bit
AS
BEGIN

    IF ISNULL(@ForecastId, 0) = 0
    BEGIN
        INSERT INTO NoaaForecasts
            (NoaaSiteId,
             Created, 
             Description, 
             County, 
             State, 
             Latitude,
             Longitude, 
             Elevation, 
             BankFullStage,
             FloodStage,
             CurrentWaterHeight,
             CurrentDischarge)
        VALUES
            (@NoaaSiteId,
             @Created, 
             @Description, 
             @County, 
             @State, 
             @Latitude,
             @Longitude, 
             @Elevation, 
             @BankFullStage,
             @FloodStage,
             @CurrentWaterHeight,
             @CurrentDischarge)
             
        SELECT @@IDENTITY AS ForecastId
    END
    ELSE
    BEGIN
        UPDATE NoaaForecasts SET
             NoaaSiteId = @NoaaSiteId,
             Created = @Created, 
             Description = @Description, 
             County = @County, 
             State = @State, 
             Latitude = @Latitude,
             Longitude = @Longitude, 
             Elevation = @Elevation, 
             BankFullStage = @BankFullStage,
             FloodStage = @FloodStage,
             CurrentWaterHeight = @CurrentWaterHeight,
             CurrentDischarge = @CurrentDischarge
         WHERE ForecastId = @ForecastId
        
        IF @PurgeData = 1
        BEGIN
            DELETE FROM NoaaForecastData WHERE ForecastId=@ForecastId
        END

        SELECT @ForecastId AS ForecastId
    END
END
GO
