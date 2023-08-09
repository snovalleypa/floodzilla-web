SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [EmailLog]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [Timestamp] datetime not null,
	[MachineName] [varchar](50) not null,
    [FromAddress] varchar(100) not null,
    [ToAddress] varchar(100) not null,
    [Subject] nvarchar(200) not null,
    [Text] text not null,
    [TextIsHtml] bit not null,
    [Result] varchar(50) null,
    [Details] varchar(500) null
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

