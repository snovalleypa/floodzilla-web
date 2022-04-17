CREATE PROCEDURE [SaveSiteError]
(
    @Timestamp datetime,
    @Severity varchar(32),
    @Source varchar(32),
    @Error text
)
AS
BEGIN
    INSERT INTO SiteErrors (Timestamp, Severity, Source, Error)
                VALUES (@Timestamp, @Severity, @Source, @Error)
END
