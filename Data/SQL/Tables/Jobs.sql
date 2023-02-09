/****** Object:  Table [dbo].[Jobs]    Script Date: 2/8/2023 9:35:42 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Jobs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobName] [nvarchar](50) NOT NULL,
	[FriendlyName] [nvarchar](200) NOT NULL,
	[LastStartTime] [datetime] NULL,
	[LastEndTime] [datetime] NULL,
	[LastSuccessfulEndTime] [datetime] NULL,
	[LastRunStatus] [nvarchar](50) NULL,
  [LastRunSummary] [nvarchar](500) NULL,
  [LastRunLogId] [int] NULL,
	[LastErrorTime] [datetime] NULL,
	[LastError] [nvarchar](200) NULL,
	[LastFullException] [text] NULL,
	[IsEnabled] [bit] NOT NULL,
	[DisableReason] [nvarchar](200) NULL,
	[DisabledTime] [datetime] NULL,
	[DisabledBy] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Jobs] ADD  CONSTRAINT [Jobs_DefaultIsEnabled]  DEFAULT ((1)) FOR [IsEnabled]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Job_JobName] ON [dbo].[Jobs]
(
	[JobName] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [Jobs]  WITH CHECK ADD  CONSTRAINT [FK_Jobs_LastRunLogId] FOREIGN KEY([LastRunLogId])
REFERENCES [JobRunLogs] ([Id])
GO
