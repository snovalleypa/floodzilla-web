/****** Object:  Table [CurveFitFormulas]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [CurveFitFormulas](
	[CalibrationId] [int] NOT NULL,
	[Constant] [float] NULL,
	[x1] [float] NULL,
	[x2] [float] NULL,
	[x3] [float] NULL,
	[x4] [float] NULL,
	[x5] [float] NULL,
	[IsDefault] [bit] NULL,
 CONSTRAINT [PK_CurveFitFormulas] PRIMARY KEY CLUSTERED 
(
	[CalibrationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
