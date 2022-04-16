/****** Object:  Table [StageDischarge]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [StageDischarge](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CalibrationId] [int] NOT NULL,
	[WaterHeight] [float] NOT NULL,
	[Discharge] [float] NOT NULL,
 CONSTRAINT [PK_StageDischarge] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [StageDischarge]  WITH CHECK ADD  CONSTRAINT [FK_StageDischarge_Calibrations] FOREIGN KEY([CalibrationId])
REFERENCES [Calibrations] ([CalibrationId])
ON DELETE CASCADE
GO
ALTER TABLE [StageDischarge] CHECK CONSTRAINT [FK_StageDischarge_Calibrations]
GO
