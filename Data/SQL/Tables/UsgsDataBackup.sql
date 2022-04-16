/****** Object:  Table [UsgsDataBackup]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [UsgsDataBackup](
	[ObservedOn] [datetime] NOT NULL,
	[SiteId] [int] NOT NULL,
	[SteamFlow] [float] NULL,
	[GageHeight] [float] NULL,
	[UsgsDataId] [int] IDENTITY(1,1) NOT NULL
) ON [PRIMARY]
GO
