/****** Object:  Table [HydroForecast]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HydroForecast](
	[ForecastId] [int] IDENTITY(1,1) NOT NULL,
	[ForecastFor] [datetime] NULL,
	[FetchId] [int] NULL,
	[Flow] [float] NULL,
	[Stage] [float] NULL,
 CONSTRAINT [PK_HydroForecast] PRIMARY KEY CLUSTERED 
(
	[ForecastId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_HydroForecast]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_HydroForecast] ON [HydroForecast]
(
	[FetchId] ASC,
	[ForecastFor] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
