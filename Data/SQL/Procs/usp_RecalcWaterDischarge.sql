/****** Object:  StoredProcedure [usp_RecalcWaterDischarge]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_RecalcWaterDischarge]
	@calibid int
AS
BEGIN
	SET NOCOUNT ON;

	declare @isdefault as bit
	declare @locid as int
	declare @x5 as float, @x4 as float, @x3 as float, @x2 as float, @x1 as float, @constant as float

	SELECT @isdefault = ISNULL(IsDefault,0), @locid = LocationId FROM Calibrations WHERE CalibrationId = @calibid

	if (@isdefault = 1)
	begin
		SELECT @x5=x5, @x4=x4, @x3=x3, @x2=x2, @x1=x1, @constant = Constant FROM CurveFitFormulas WHERE CalibrationId = @calibid

		UPDATE FzLevel SET CalibrationId = @calibid, WaterDischarge = dbo.udf_GetWaterDischarge(@x5, @x4, @x3, @x2, @x1, @constant, WaterHeight/12.0) WHERE (Iteration = 5 OR Iteration = 6) AND LocationId = @locid
	end
END
GO
