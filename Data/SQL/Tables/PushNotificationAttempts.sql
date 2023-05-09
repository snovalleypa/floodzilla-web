SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [PushNotificationAttempts]
(
	[Id] int IDENTITY(1,1) NOT NULL,
    [Token] varchar(4096) NOT NULL,
    [Title] NVARCHAR(128) NULL,
    [Subtitle] NVARCHAR(128) NULL,
    [Body] NVARCHAR(1024) NULL,
    [Data] NVARCHAR(2048) NULL,
    [Status] int NOT NULL,
    [SendTime] datetime NOT NULL,
    [TicketId] varchar(256) NULL,
    [LastCheckTime] datetime NOT NULL,
    [LastRetrySeconds] int NULL,
    [NextActiveTime] datetime NULL
) ON [PRIMARY]
GO

