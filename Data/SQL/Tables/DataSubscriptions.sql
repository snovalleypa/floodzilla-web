/****** Object:  Table [DataSubscriptions]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [DataSubscriptions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[FzPostUrl] [varchar](max) NOT NULL,
	[IsSubscribe] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_DataSubscriptions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_UserId] UNIQUE NONCLUSTERED 
(
	[UserId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [DataSubscriptions] ADD  CONSTRAINT [DF_DataSubscriptions_IsSubscribe]  DEFAULT ((1)) FOR [IsSubscribe]
GO
ALTER TABLE [DataSubscriptions] ADD  CONSTRAINT [DF_DataSubscriptions_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [DataSubscriptions]  WITH CHECK ADD  CONSTRAINT [FK_DataSubscriptions_Users] FOREIGN KEY([UserId])
REFERENCES [Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [DataSubscriptions] CHECK CONSTRAINT [FK_DataSubscriptions_Users]
GO
