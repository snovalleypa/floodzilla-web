/****** Object:  Table [dbo].[SenixListenerLog]    Script Date: 12/22/2019 8:08:30 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SenixListenerLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Timestamp] [datetime] NOT NULL,
    [ListenerInfo] [varchar](200) NULL,
    [ClientIP] [varchar](64) NULL,
	[ExternalDeviceId] [varchar](70) NULL,
	[ReceiverId] [varchar](70) NULL,
    [DeviceId] int NULL,
    [ReadingId] int NULL,
	[RawSensorData] [text] NOT NULL,
	[Result] [nvarchar](200) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


ALTER TABLE SenixListenerLog ADD   CONSTRAINT PK_SenixListenerLog PRIMARY KEY CLUSTERED (Id ASC)
GO

CREATE NONCLUSTERED INDEX [Ix_SenixListenerLog] ON [SenixListenerLog]
(
    [DeviceId] ASC,
    [Timestamp] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
