/****** Object:  Table [Regions]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Regions](
	[RegionId] [int] IDENTITY(1,1) NOT NULL,
	[RegionName] [nvarchar](150) NULL,
	[Address] [nvarchar](256) NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[WindowsTimeZone] [nvarchar](64) NULL,
	[IanaTimeZone] [nvarchar](64) NULL,
	[RecentWeatherId] [int] NULL,
	[HydroSourceURL] [nvarchar](512) NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[IsPublic] [bit] NOT NULL,
	[OrganizationsId] [int] NOT NULL,
	[BaseURL] [nvarchar](400) NULL,
	[SmsFormatBaseURL] [nvarchar](400) NULL,
	[TempADCTestsCount] [int] NULL,
	[DefaultADCTestsCount] [int] NULL,
	[TempADCTestsCountValidTill] [datetime] NULL,
	[IsDefault] [bit] NOT NULL,
	[NotifyList] [nvarchar](256) NULL,
	[SensorOfflineThreshold] [int] NULL,
	[SlackNotifyUrl] [nvarchar](256) NULL,
    [DefaultForecastGageList] [nvarchar](256) NULL,
 CONSTRAINT [PK__Regions__3214EC07A4B42177] PRIMARY KEY CLUSTERED 
(
	[RegionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [Regions] ADD  CONSTRAINT [DF_Regions_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [Regions] ADD  CONSTRAINT [DF_Regions_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [Regions] ADD  CONSTRAINT [DF_Regions_IsPublic]  DEFAULT ((0)) FOR [IsPublic]
GO
ALTER TABLE [Regions] ADD  CONSTRAINT [DF_Regions_IsDefault]  DEFAULT ((0)) FOR [IsDefault]
GO
ALTER TABLE [Regions]  WITH CHECK ADD  CONSTRAINT [FK_Regions_Organizations] FOREIGN KEY([OrganizationsId])
REFERENCES [Organizations] ([OrganizationsID])
GO
ALTER TABLE [Regions] CHECK CONSTRAINT [FK_Regions_Organizations]
GO
