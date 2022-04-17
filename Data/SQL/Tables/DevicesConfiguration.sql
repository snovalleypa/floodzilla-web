/****** Object:  Table [DevicesConfiguration]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [DevicesConfiguration](
	[DeviceId] [int] NOT NULL,
	[MinutesBetweenBatches] [int] NOT NULL,
	[SecBetweenADCSense] [int] NOT NULL,
	[ADCTestsCount] [int] NULL,
	[SenseIterationMinutes] [int] NULL,
	[SendIterationCount] [int] NULL,
	[GPSIterationCount] [int] NULL,
	[NotifyConsecIncreaseCount] [int] NULL,
	[NotifyConsecDecreaseCount] [int] NULL,
	[NotifyConsecIncreaseInches] [int] NULL,
	[NotifyConsecDecreaseInches] [int] NULL,
	[SensorType] [int] NULL,
	[UpdateFlashConfig] [text] NULL,
 CONSTRAINT [PK_DevicesConfiguration] PRIMARY KEY CLUSTERED 
(
	[DeviceId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [DevicesConfiguration]  WITH CHECK ADD  CONSTRAINT [FK_DevicesConfiguration_Devices] FOREIGN KEY([DeviceId])
REFERENCES [Devices] ([DeviceId])
ON DELETE CASCADE
GO
ALTER TABLE [DevicesConfiguration] CHECK CONSTRAINT [FK_DevicesConfiguration_Devices]
GO
