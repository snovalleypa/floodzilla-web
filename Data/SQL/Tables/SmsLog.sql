SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SmsLog]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [Timestamp] datetime not null,
	[MachineName] [varchar](50) not null,
    [LogEntryType] varchar(50) not null,
    [FromNumber] varchar(100) null,
    [ToNumber] varchar(100) null,
    [Text] text null,
    [Result] varchar(50) null,
    [Details] varchar(500) null
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

