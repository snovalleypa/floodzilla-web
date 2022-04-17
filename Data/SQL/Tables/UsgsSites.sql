/****** Object:  Table [UsgsSites]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [UsgsSites](
    [SiteId] [int] NOT NULL,
    [SiteName] [nvarchar](150) NULL,
    [Latitude] [float] NULL,
    [Longitude] [float] NULL,
    [NoaaSiteId] [varchar](20) NULL,
    [NotifyForecasts] bit NOT NULL,
 CONSTRAINT [PK_UsgsSites] PRIMARY KEY CLUSTERED 
(
    [SiteId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [Users] ADD  DEFAULT ((0)) FOR [NotifyForecasts]
