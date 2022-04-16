/****** Object:  Table [SubDomainsConfig]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SubDomainsConfig](
	[SubDomainId] [int] NOT NULL,
	[ConfigName] [nvarchar](256) NOT NULL,
	[ConfigValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_SubDomainsConfig] PRIMARY KEY CLUSTERED 
(
	[SubDomainId] ASC,
	[ConfigName] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
