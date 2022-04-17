/****** Object:  UserDefinedFunction [udf_GetFloodLevel]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [udf_GetFloodLevel](@height float, @green float, @brown float, @yellow float) returns nvarchar(50) AS
BEGIN
	declare @floodlevel nvarchar(50) = 'Normal';
	
	if (@height >= @green and @height < @brown)
	begin 
        set @floodlevel = 'Level 1'
	end
    else if (@height >= @brown and @height < @yellow)
	begin
        set @floodlevel = 'Level 2'
    end
	else if (@height >= @yellow)
	begin
        set @floodlevel = 'Level 3'
	end

	return @floodlevel
END
GO
