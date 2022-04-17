SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SiteSettings]
(
  [RegionId] int not null,
  [SiteAdminEmail] varchar(256) not null,
  [SiteAdminSlackUrl] varchar(256) not null
) ON [PRIMARY]
GO
