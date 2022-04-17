/****** Object:  Table [Elevations]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Elevations](
	[ElevationId] [int] IDENTITY(1,1) NOT NULL,
	[ElevationName] [varchar](max) NOT NULL,
	[ElevationTypeId] [int] NOT NULL,
	[LocationId] [int] NOT NULL,
	[Elevation] [float] NOT NULL,
 CONSTRAINT [PK_Elevations] PRIMARY KEY CLUSTERED 
(
	[ElevationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [Elevations]  WITH CHECK ADD  CONSTRAINT [FK_Elevations_ElevationTypes] FOREIGN KEY([ElevationTypeId])
REFERENCES [ElevationTypes] ([ElevationTypeId])
ON DELETE CASCADE
GO
ALTER TABLE [Elevations] CHECK CONSTRAINT [FK_Elevations_ElevationTypes]
GO
