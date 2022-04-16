/****** Object:  Table [UserNotifications]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [UserNotifications](
	[UserId] [int] NOT NULL,
	[ChannelTypeId] [int] NOT NULL,
	[NotifyTypeId] [int] NOT NULL,
	[NotifyId] [int] IDENTITY(1,1) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_UserNotifications] PRIMARY KEY CLUSTERED 
(
	[NotifyId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [UserNotifications] ADD  CONSTRAINT [DF_UserNotifications_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [UserNotifications]  WITH CHECK ADD  CONSTRAINT [FK_UserNotifications_ChannelTypes] FOREIGN KEY([ChannelTypeId])
REFERENCES [ChannelTypes] ([ChannelTypeId])
GO
ALTER TABLE [UserNotifications] CHECK CONSTRAINT [FK_UserNotifications_ChannelTypes]
GO
ALTER TABLE [UserNotifications]  WITH CHECK ADD  CONSTRAINT [FK_UserNotifications_NotifyTypes] FOREIGN KEY([NotifyTypeId])
REFERENCES [NotifyTypes] ([NotifyTypeId])
GO
ALTER TABLE [UserNotifications] CHECK CONSTRAINT [FK_UserNotifications_NotifyTypes]
GO
ALTER TABLE [UserNotifications]  WITH CHECK ADD  CONSTRAINT [FK_UserNotifications_Users] FOREIGN KEY([UserId])
REFERENCES [Users] ([Id])
GO
ALTER TABLE [UserNotifications] CHECK CONSTRAINT [FK_UserNotifications_Users]
GO
