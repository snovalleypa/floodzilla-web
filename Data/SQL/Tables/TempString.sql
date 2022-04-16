/****** Object:  Table [TempString]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [TempString](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[data] [text] NULL,
	[CreatedOn] [datetime] NOT NULL,
 CONSTRAINT [PK_TempString] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [TempString] ADD  CONSTRAINT [DF_TempString_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO
