/****** Object:  Table [Organizations]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Organizations](
	[OrganizationsID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](30) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_Organizations] PRIMARY KEY CLUSTERED 
(
	[OrganizationsID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [Organizations] ADD  CONSTRAINT [DF_Organizations_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [Organizations] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
