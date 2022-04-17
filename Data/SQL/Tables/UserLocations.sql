/****** Object:  Table [UserLocations]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [UserLocations](
	[UserId] [int] NOT NULL,
	[LocationId] [int] NOT NULL,
	[RegionId] [int] NOT NULL,
 CONSTRAINT [PK_CompositeKeys] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LocationId] ASC,
	[RegionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [UserLocations]  WITH CHECK ADD  CONSTRAINT [UserLocation_Regions] FOREIGN KEY([RegionId])
REFERENCES [Regions] ([RegionId])
GO
ALTER TABLE [UserLocations] CHECK CONSTRAINT [UserLocation_Regions]
GO
ALTER TABLE [UserLocations]  WITH CHECK ADD  CONSTRAINT [UserLocation_Users] FOREIGN KEY([UserId])
REFERENCES [Users] ([Id])
GO
ALTER TABLE [UserLocations] CHECK CONSTRAINT [UserLocation_Users]
GO
