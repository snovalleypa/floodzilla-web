SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [PushDeviceLog]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [Timestamp] datetime not null,
	[MachineName] [varchar](50) not null,
    [LogEntryType] varchar(50) not null,
    [Token] varchar(4096) null,
    [UserId] int null,
    [Platform] varchar(64) null,
    [Language] varchar(64) null,
    [Extra] text null
) ON [PRIMARY]
GO

