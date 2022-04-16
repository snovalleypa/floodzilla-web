/****** Object:  Table [ChannelTypes]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ChannelTypes](
	[ChannelTypeId] [int] NOT NULL,
	[ChannelTypeName] [nvarchar](50) NULL,
 CONSTRAINT [PK_ChannalTypes] PRIMARY KEY CLUSTERED 
(
	[ChannelTypeId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
