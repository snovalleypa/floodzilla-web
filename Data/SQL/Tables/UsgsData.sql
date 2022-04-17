/****** Object:  Table [UsgsData]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [UsgsData](
	[ObservedOn] [datetime] NOT NULL,
	[SiteId] [int] NOT NULL,
	[SteamFlow] [float] NULL,
	[GageHeight] [float] NULL,
	[UsgsDataId] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_UsgsData] PRIMARY KEY CLUSTERED 
(
	[UsgsDataId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_UsgsData]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_UsgsData] ON [UsgsData]
(
	[ObservedOn] ASC,
	[SiteId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
