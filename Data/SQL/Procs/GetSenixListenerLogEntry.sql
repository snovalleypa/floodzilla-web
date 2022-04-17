CREATE PROCEDURE [GetSenixListenerLogEntry]
    @Id int
AS
BEGIN

	SELECT * from SenixListenerLog
		WHERE (Id = @Id)
END
