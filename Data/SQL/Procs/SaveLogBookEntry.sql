CREATE PROCEDURE [SaveLogBookEntry]
(
    @Id int,
    @UserId int,
    @Timestamp datetime,
    @Text text,
    @IsDeleted bit,
    @TagList nvarchar(2048)
)
AS
BEGIN

    IF (@Id = 0)
    BEGIN
        INSERT INTO LogBookEntries (UserId, Timestamp, Text, IsDeleted)
                    VALUES (@UserId, @Timestamp, @Text, @IsDeleted)
        SELECT @Id = @@IDENTITY
    END
    ELSE
    BEGIN
        UPDATE LogBookEntries SET UserId=@UserId, Timestamp=@Timestamp, Text=@Text, IsDeleted=@IsDeleted WHERE Id=@Id
        DELETE FROM LogBookEntryTags WHERE Id=@Id
    END

    INSERT INTO LogBookEntryTags SELECT @Id, value FROM STRING_SPLIT(@TagList, ',')

    SELECT @Id As Id
END
