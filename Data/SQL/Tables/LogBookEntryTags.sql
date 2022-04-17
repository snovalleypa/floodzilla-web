CREATE TABLE [LogBookEntryTags]
(
	[Id] int NOT NULL,
    [Tag] nvarchar(100) NOT NULL,
)
GO

ALTER TABLE [LogBookEntryTags]  WITH CHECK ADD  CONSTRAINT [FK_LogBookEntryTags_LogBookEntries] FOREIGN KEY([Id])
REFERENCES [LogBookEntries] ([Id])
GO

