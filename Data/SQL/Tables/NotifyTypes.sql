/****** Object:  Table [NotifyTypes]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [NotifyTypes](
	[NotifyTypeId] [int] NOT NULL,
	[NotifyTypeName] [nvarchar](150) NULL,
 CONSTRAINT [PK_NotifyTypes] PRIMARY KEY CLUSTERED 
(
	[NotifyTypeId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
