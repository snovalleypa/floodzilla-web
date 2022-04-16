

CREATE PROCEDURE [GetDistinctLogBookTags]
AS
BEGIN
    SELECT DISTINCT T.Tag FROM LogBookEntryTags T
        INNER JOIN LogBookEntries E on E.Id = T.Id
        WHERE T.Tag like '#%'
        AND E.IsDeleted = 0

END
