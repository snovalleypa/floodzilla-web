CREATE TABLE [NoaaForecasts]
(
    [ForecastId] [int] IDENTITY(1,1) NOT NULL,
    [NoaaSiteId] [varchar](100) NOT NULL,
    [Created] [datetime] NOT NULL,
    [Description] [nvarchar](200) NULL,
    [County] [nvarchar](100) NULL,
    [State] [nvarchar](100) NULL,
    [Latitude] [float] NULL,
    [Longitude] [float] NULL,
    [Elevation] [float] NULL,
    [BankFullStage] [float] NULL,
    [FloodStage] [float] NULL,
    [CurrentWaterHeight] [float] NULL,
    [CurrentDischarge] [float] NULL,
 CONSTRAINT [PK_NoaaForecasts] PRIMARY KEY CLUSTERED 
(
    [ForecastId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
