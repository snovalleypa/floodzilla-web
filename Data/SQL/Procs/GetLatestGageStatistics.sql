SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [GetLatestGageStatistics]
	@locationId int
AS
BEGIN

	SELECT top 1 * from GageStatistics
		WHERE LocationId = @locationId
		ORDER BY Date DESC
END
GO
