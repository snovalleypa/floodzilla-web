/****** Object:  Table [NotifyGroups]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [NotifyGroups](
	[NotifyId] [int] NOT NULL,
	[GroupId] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_NotifyGroups] PRIMARY KEY CLUSTERED 
(
	[GroupId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [NotifyGroups]  WITH CHECK ADD  CONSTRAINT [FK_NotifyGroups_UserNotifications] FOREIGN KEY([NotifyId])
REFERENCES [UserNotifications] ([NotifyId])
ON DELETE CASCADE
GO
ALTER TABLE [NotifyGroups] CHECK CONSTRAINT [FK_NotifyGroups_UserNotifications]
GO
