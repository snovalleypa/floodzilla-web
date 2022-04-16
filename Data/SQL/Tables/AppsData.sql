/****** Object:  Table [AppsData]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [AppsData](
	[AppsDataId] [int] IDENTITY(1,1) NOT NULL,
	[ExternalId] [nvarchar](128) NULL,
	[AppId] [int] NULL,
	[LocationId] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[AppData] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppsData] PRIMARY KEY CLUSTERED 
(
	[AppsDataId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [AppsData] ADD  CONSTRAINT [DF_AppsData_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO
ALTER TABLE [AppsData] ADD  CONSTRAINT [DF_AppsData_ModifiedOn]  DEFAULT (getutcdate()) FOR [ModifiedOn]
GO
