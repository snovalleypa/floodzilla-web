/****** Object:  Table [Weathers]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Weathers](
	[WeatherId] [int] IDENTITY(1,1) NOT NULL,
	[RegionId] [int] NULL,
	[Timestamp] [datetime] NULL,
	[WeatherStatus] [nvarchar](150) NULL,
	[Temperature] [float] NULL,
	[Precip1HourMM] [float] NULL,
	[ResponseString] [nvarchar](max) NULL,
 CONSTRAINT [PK_Weathers] PRIMARY KEY CLUSTERED 
(
	[WeatherId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [RegionsId]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [RegionsId] ON [Weathers]
(
	[RegionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [weather_timestamp]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [weather_timestamp] ON [Weathers]
(
	[Timestamp] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
ALTER TABLE [Weathers] ADD  CONSTRAINT [DF_Weathers_Timestamp]  DEFAULT (getutcdate()) FOR [Timestamp]
GO
