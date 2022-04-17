/****** Object:  Table [SensorReadingsBackup]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SensorReadingsBackup](
	[ListenerInfo] [varchar](200) NULL,
	[Timestamp] [datetime] NULL,
	[DeviceTimestamp] [datetime] NULL,
	[LocationId] [int] NULL,
	[DeviceId] [int] NULL,
	[GroundHeight] [float] NULL,
	[DistanceReading] [float] NULL,
	[WaterHeight] [float] NULL,
	[WaterDischarge] [float] NULL,
	[BatteryVolt] [int] NULL,
	[RawSensorData] [text] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IsDeleted] [int] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
