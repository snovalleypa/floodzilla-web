SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SiteErrors]
(
    [Timestamp] datetime not null,
    [Severity] varchar(32) not null,
    [Source] varchar(128) not null,
    [Error] text not null
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
