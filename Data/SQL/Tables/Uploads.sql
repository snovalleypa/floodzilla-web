/****** Object:  Table [Uploads]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Uploads](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DateOfPicture] [datetime] NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[Altitude] [float] NULL,
	[LocationId] [int] NOT NULL,
	[EventId] [int] NULL,         --- TODO: Remove this column
	[ResponseString] [nvarchar](max) NULL,
	[Image] [varchar](max) NOT NULL,
	[IsVarified] [bit] NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[Rank] [int] NULL,
 CONSTRAINT [PK__Uploads__3214EC07AC19BE6A] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [Uploads] ADD  CONSTRAINT [DF__Uploads__DateOfP__6442E2C9]  DEFAULT (getdate()) FOR [DateOfPicture]
GO
ALTER TABLE [Uploads] ADD  CONSTRAINT [DF__Uploads__IsVarif__690797E6]  DEFAULT ('0') FOR [IsVarified]
GO
ALTER TABLE [Uploads] ADD  CONSTRAINT [DF_Uploads_IsActive]  DEFAULT ((0)) FOR [IsActive]
GO
ALTER TABLE [Uploads] ADD  CONSTRAINT [DF_Uploads_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
