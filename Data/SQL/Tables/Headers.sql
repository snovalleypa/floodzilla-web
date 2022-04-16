/****** Object:  Table [Headers]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Headers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Timestamp] [datetime] NOT NULL,
	[DeviceId] [int] NOT NULL,
	[Version] [int] NULL,
	[Count] [int] NULL,
	[BatteryVolt] [int] NULL,
	[HeaterVolt] [int] NULL,
	[HeaterOnBatteryVolt] [int] NULL,
	[TimeBetweenADC] [int] NULL,
	[HeaterOnTime] [int] NULL,
	[StartTempTop] [int] NULL,
	[StartTempBottom] [int] NULL,
	[WeatherId] [int] NULL,
	[Note] [nvarchar](512) NULL,
	[AggNote] [nvarchar](512) NULL,
	[LocationId] [int] NULL,
	[ModifiedOn] [datetime] NULL,
	[GroundHeight] [float] NULL,
	[UsgsDataId] [int] NULL,
	[DeviceTimeStamp] [datetime] NULL,
	[ICCID4] [int] NULL,
	[ICCIDLAST4] [int] NULL,
 CONSTRAINT [PK_Headers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [Headers] ADD  CONSTRAINT [DF_Headers_Timestamp]  DEFAULT (getutcdate()) FOR [Timestamp]
GO
ALTER TABLE [Headers] ADD  CONSTRAINT [DF_Headers_DeviceId]  DEFAULT ((1)) FOR [DeviceId]
GO
