/****** Object:  Table [TempSensorTypes]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [TempSensorTypes](
	[Id] [int] NOT NULL,
	[Rating] [nvarchar](50) NULL,
	[CurveJson] [nvarchar](max) NULL,
 CONSTRAINT [PK_TempSensorTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
