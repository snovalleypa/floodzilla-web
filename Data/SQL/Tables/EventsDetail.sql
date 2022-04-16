/****** Object:  Table [EventsDetail]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [EventsDetail](
	[EventId] [int] NOT NULL,
	[LocationId] [int] NOT NULL,
 CONSTRAINT [PK_EventDetail] PRIMARY KEY CLUSTERED 
(
	[EventId] ASC,
	[LocationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [EventsDetail]  WITH CHECK ADD  CONSTRAINT [FK_EventsDetail_FloodEvents] FOREIGN KEY([EventId])
REFERENCES [FloodEvents] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [EventsDetail] CHECK CONSTRAINT [FK_EventsDetail_FloodEvents]
GO
ALTER TABLE [EventsDetail]  WITH CHECK ADD  CONSTRAINT [FK_EventsDetail_Locations] FOREIGN KEY([LocationId])
REFERENCES [Locations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [EventsDetail] CHECK CONSTRAINT [FK_EventsDetail_Locations]
GO
