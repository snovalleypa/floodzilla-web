/****** Object:  Table [Users]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Users](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [AspNetUserId] [nvarchar](450) NOT NULL,
    [FirstName] [varchar](250) NOT NULL,
    [LastName] [varchar](250) NOT NULL,
    [Address] [varchar](250) NULL,
    [OrganizationsID] [int] NULL,
    [IsDeleted] [bit] NOT NULL,
    [CellPhone] [nvarchar](50) NULL,
    [DeviceToken] [nvarchar](max) NULL,
    [NotifyViaEmail] bit NOT NULL,
    [NotifyViaSms] bit NOT NULL,
    [NotifyDailyForecasts] bit NOT NULL,
    [NotifyForecastAlerts] bit NOT NULL,
    [CreatedOn] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [aspuseruD] UNIQUE NONCLUSTERED 
(
    [AspNetUserId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [Users] ADD  DEFAULT ((0)) FOR [IsDeleted]
ALTER TABLE [Users] ADD  DEFAULT ((0)) FOR [NotifyViaEmail]
ALTER TABLE [Users] ADD  DEFAULT ((0)) FOR [NotifyViaSms]
ALTER TABLE [Users] ADD  DEFAULT ((0)) FOR [NotifyDailyForecasts]
ALTER TABLE [Users] ADD  DEFAULT ((0)) FOR [NotifyForecastAlerts]
GO
ALTER TABLE [Users]  WITH CHECK ADD  CONSTRAINT [FK_Organization] FOREIGN KEY([OrganizationsID])
REFERENCES [Organizations] ([OrganizationsID])
GO
ALTER TABLE [Users] CHECK CONSTRAINT [FK_Organization]
GO
ALTER TABLE [Users]  WITH CHECK ADD  CONSTRAINT [FK_Users] FOREIGN KEY([AspNetUserId])
REFERENCES [AspNetUsers] ([Id])
GO
ALTER TABLE [Users] CHECK CONSTRAINT [FK_Users]
GO
ALTER TABLE [Users] ADD  CONSTRAINT [DF_Users_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

