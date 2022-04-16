CREATE TABLE [LogBookEntries]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [UserId] [int] NOT NULL,
	[Timestamp] [datetime] NOT NULL,
    [Text] [text] NULL,

	[IsDeleted] [bit] NOT NULL,

    CONSTRAINT [PK_LogBookEntries] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
GO

ALTER TABLE [LogBookEntries]  WITH CHECK ADD  CONSTRAINT [FK_LogBookEntries_Users] FOREIGN KEY([UserId])
REFERENCES [Users] ([Id])
GO

ALTER TABLE [LogBookEntries] ADD  CONSTRAINT [DF_LogBookEntries_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO

