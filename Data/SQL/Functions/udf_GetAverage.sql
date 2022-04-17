/****** Object:  UserDefinedFunction [udf_GetAverage]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [udf_GetAverage](
@L1 float,
@L2 float,
@L3 float,
@L4 float,
@L5 float,
@L6 float,
@L7 float,
@L8 float,
@L9 float,
@L10 float,
@AvgL float) RETURNS float AS
BEGIN
	declare @avg float = 0, @sum float = 0, @count float = 0

	if (not @AvgL is null)
	begin
		set @avg =  @AvgL
	end
	else
	begin
		set @sum = iif(@L1>=12, @L1, 0) + iif(@L2>=12, @L2, 0) + iif(@L3>=12, @L3, 0) + iif(@L4>=12, @L4, 0) + iif(@L5>=12, @L5, 0) + iif(@L6>=12, @L6, 0) + iif(@L7>=12, @L7, 0) + iif(@L8>=12, @L8, 0) + iif(@L9>=12, @L9, 0) + iif(@L10>=12, @L10, 0)
		if (@sum > 0) set @count = iif(@L1>=12, 1.0, 0) + iif(@L2>=12, 1.0, 0) + iif(@L3>=12, 1.0, 0) + iif(@L4>=12, 1.0, 0) + iif(@L5>=12, 1.0, 0) + iif(@L6>=12, 1.0, 0) + iif(@L7>=12, 1.0, 0) + iif(@L8>=12, 1.0, 0) + iif(@L9>=12, 1.0, 0) + iif(@L10>=12, 1.0, 0);
		if(@sum > 0 and @count > 0) set @avg = @sum / @count; 
	end

	return(@avg)
END
GO
