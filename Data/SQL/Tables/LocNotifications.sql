/****** Object:  Table [LocNotifications]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [LocNotifications](
	[UserId] [int] NOT NULL,
	[ChannelTypeId] [int] NOT NULL,
	[NotifyTypeId] [int] NOT NULL,
	[LocationId] [int] NOT NULL,
	[NotifyId] [int] NOT NULL,
	[Level1SentOn] [datetime] NULL,
	[Level2SentOn] [datetime] NULL,
	[Level3SentOn] [datetime] NULL,
 CONSTRAINT [PK_LocNotifications] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[ChannelTypeId] ASC,
	[NotifyTypeId] ASC,
	[LocationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [LocNotifications]  WITH CHECK ADD  CONSTRAINT [FK_LocNotifications_UserNotifications] FOREIGN KEY([NotifyId])
REFERENCES [UserNotifications] ([NotifyId])
ON DELETE CASCADE
GO
ALTER TABLE [LocNotifications] CHECK CONSTRAINT [FK_LocNotifications_UserNotifications]
GO
