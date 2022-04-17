/****** Object:  StoredProcedure [usp_InsertHydroUpdate]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [usp_InsertHydroUpdate]
	@RegionId int,
	@FetchId int = null output
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO HydroUpdates(RegionId) VALUES (@RegionId);
	SET @FetchId = SCOPE_IDENTITY();
END
GO
