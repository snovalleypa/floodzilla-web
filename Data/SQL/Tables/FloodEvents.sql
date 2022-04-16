/****** Object:  Table [FloodEvents]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [FloodEvents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventName] [varchar](100) NOT NULL,
	[FromDate] [date] NOT NULL,
	[ToDate] [date] NOT NULL,
	[RegionId] [int] NOT NULL,
	[LocationIds] [varchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[IsPublic] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [FloodEvents] ADD  CONSTRAINT [DF_FloodEvents_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [FloodEvents] ADD  CONSTRAINT [DF_FloodEvents_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [FloodEvents] ADD  CONSTRAINT [DF_FloodEvents_IsPublic]  DEFAULT ((0)) FOR [IsPublic]
GO
ALTER TABLE [FloodEvents]  WITH CHECK ADD  CONSTRAINT [FK__FloodEven__Regio__4F47C5E3] FOREIGN KEY([RegionId])
REFERENCES [Regions] ([RegionId])
GO
ALTER TABLE [FloodEvents] CHECK CONSTRAINT [FK__FloodEven__Regio__4F47C5E3]
GO
