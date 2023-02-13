CREATE PROCEDURE [EnsureJob]
(
    @JobName nvarchar(50),
    @FriendlyName nvarchar(200)
)
AS
BEGIN

    IF NOT EXISTS (SELECT Id FROM Jobs where JobName=@JobName)
    BEGIN
        INSERT INTO Jobs (JobName, FriendlyName)
                    VALUES (@JobName, @FriendlyName)
    END

    SELECT * FROM Jobs WHERE JobName=@JobName
END
