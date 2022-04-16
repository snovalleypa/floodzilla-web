/****** Object:  UserDefinedFunction [udf_GetBatteryStatus]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [udf_GetBatteryStatus](@BatteryVolt float) RETURNS FLOAT AS
BEGIN
	declare @percentage float = 0
	
	if (@BatteryVolt >= 4200)
	begin 
        set @percentage = 100
	end
    else if (@BatteryVolt <= 3200)
	begin
        set @percentage = 0
    end
	else
	begin
        set @percentage = (@BatteryVolt - 3700.0) * 100.0 / (500.0)
	end

	return @percentage
END
GO
