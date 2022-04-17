/****** Object:  StoredProcedure [MarkLogBookEntriesAsDeleted]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [MarkLogBookEntriesAsDeleted]
(
	@IdList varchar(200)
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    -- Insert statements for procedure here
    UPDATE LogBookEntries SET IsDeleted=1 WHERE Id IN (SELECT value FROM string_split(@IdList,',')) 
END
GO
