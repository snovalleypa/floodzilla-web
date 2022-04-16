/****** Object:  Table [Calibrations]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Calibrations](
	[CalibrationId] [int] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](100) NOT NULL,
	[LocationId] [int] NOT NULL,
	[IsDefault] [bit] NOT NULL,
	[CalibratedBy] [int] NOT NULL,
	[Note] [varchar](max) NULL,
 CONSTRAINT [PK_Calibritions] PRIMARY KEY CLUSTERED 
(
	[CalibrationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
