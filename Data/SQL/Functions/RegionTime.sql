SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [RegionTime](@regionId int, @dt datetime)
RETURNS datetime
AS
BEGIN
    DECLARE @tz NVARCHAR(64)
    SELECT @tz = WindowsTimeZone FROM Regions WHERE RegionId=@regionId
    RETURN @dt AT TIME ZONE @tz
END
GO
