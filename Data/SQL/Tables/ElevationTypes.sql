/****** Object:  Table [ElevationTypes]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ElevationTypes](
	[ElevationTypeId] [int] IDENTITY(1,1) NOT NULL,
	[ElevationTypeName] [varchar](100) NOT NULL,
 CONSTRAINT [PK_ElevationTypes] PRIMARY KEY CLUSTERED 
(
	[ElevationTypeId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
