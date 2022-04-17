/****** Object:  UserDefinedFunction [udf_GetWaterDischarge]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [udf_GetWaterDischarge](@x5 float, @x4 float, @x3 float, @x2 float, @x1 float, @constant float, @waterheight float) RETURNS float AS
BEGIN
	declare @waterdischarge float

	set @waterdischarge	=	@x5 * Power(@waterheight, 5) +
							@x4 * Power(@waterheight, 4) +
							@x3 * Power(@waterheight, 3) +
							@x2 * Power(@waterheight, 2) +
							@x1 * Power(@waterheight, 1) +
							@constant;

	return(@waterdischarge)
END
GO
