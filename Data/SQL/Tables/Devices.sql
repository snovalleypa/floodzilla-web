/****** Object:  Table [Devices]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Devices](
    [DeviceId] [int] NOT NULL,
    [Name] [varchar](200) NULL,
    [IMEI] [varchar](40) NULL,
    [Description] [nvarchar](max) NULL,
    [LocationId] [int] NULL,
    [Min] [float] NULL,
    [Max] [float] NULL,
    [MaxStDev] [float] NULL,
    [IsActive] [bit] NOT NULL,
    [IsDeleted] [bit] NOT NULL,
    [DeviceTypeId] [int] NOT NULL,
    [Version] [int] NULL,
    [TempSensorTypeId] [int] NULL,
    [ExternalDeviceId] [varchar](70) NULL,
    [LatestReceiverId] [varchar](70) NULL,
    [SensorUpdateInterval] [int] NULL,
    [LastReadingReceived] [datetime] NULL,
    [UsgsSiteId] [int] NULL,
 CONSTRAINT [PK_Devices] PRIMARY KEY CLUSTERED 
(
    [DeviceId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [LocationUnique]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [LocationUnique] ON [Devices]
(
    [LocationId] ASC
)
WHERE ([LocationId] IS NOT NULL)
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
ALTER TABLE [Devices] ADD  CONSTRAINT [DF_Devices_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [Devices] ADD  CONSTRAINT [DF_Devices_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [Devices]  WITH CHECK ADD  CONSTRAINT [FK_Devices_DeviceTypes] FOREIGN KEY([DeviceTypeId])
REFERENCES [DeviceTypes] ([DeviceTypeId])
GO
ALTER TABLE [Devices] CHECK CONSTRAINT [FK_Devices_DeviceTypes]
GO
