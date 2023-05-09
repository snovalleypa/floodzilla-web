SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [PushNotificationLog]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [Timestamp] datetime not null,
	[MachineName] [varchar](50) not null,
    [LogEntryType] varchar(50) not null,
    [Tokens] text not null,
    [Title] NVARCHAR(128) null,
    [Subtitle] NVARCHAR(128) null,
    [Body] NVARCHAR(1024) null,
    [Data] NVARCHAR(2048) null,
    [Result] varchar(50) null,
    [Response] text null
) ON [PRIMARY]
GO

