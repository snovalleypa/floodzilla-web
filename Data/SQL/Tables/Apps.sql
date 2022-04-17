/****** Object:  Table [Apps]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Apps](
	[AppId] [int] IDENTITY(1,1) NOT NULL,
	[AppName] [nvarchar](256) NULL,
	[RegisteredOn] [datetime] NULL,
 CONSTRAINT [PK_Apps] PRIMARY KEY CLUSTERED 
(
	[AppId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [Apps] ADD  CONSTRAINT [DF_Apps_RegisteredOn]  DEFAULT (getutcdate()) FOR [RegisteredOn]
GO
