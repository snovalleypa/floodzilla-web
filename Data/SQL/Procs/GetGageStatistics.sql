SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetGageStatistics]
	@locationId int
AS
BEGIN

	SELECT * from GageStatistics
		WHERE LocationId = @locationId
		ORDER BY Date ASC
END
GO
