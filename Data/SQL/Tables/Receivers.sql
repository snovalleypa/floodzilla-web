/****** Object:  Table [Receivers]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Receivers](
	[ReceiverId] [int] IDENTITY(1,1) NOT NULL,
	[ExternalReceiverId] [varchar](70) NOT NULL,
	[LatestIPAddress] [varchar](70) NULL,
	[Name] [varchar](200) NULL,
	[Description] [nvarchar](200) NULL,
	[Location] [nvarchar](200) NULL,
	[ContactInfo] [nvarchar](200) NULL,
	[ConnectionInfo] [nvarchar](200) NULL,
	[SimId] [nvarchar](200) NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[IsDeleted] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ReceiverId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO

ALTER TABLE [Receivers] ADD  CONSTRAINT [DF_Receivers_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO

