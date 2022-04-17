/****** Object:  Table [SubDomains]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SubDomains](
	[SubDomainId] [int] NOT NULL,
	[SubDomainName] [nvarchar](512) NOT NULL,
	[CreatedOn] [datetime] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_SubDomains] PRIMARY KEY CLUSTERED 
(
	[SubDomainId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [SubDomains] ADD  CONSTRAINT [DF_SubDomains_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO
ALTER TABLE [SubDomains] ADD  CONSTRAINT [DF_SubDomains_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
