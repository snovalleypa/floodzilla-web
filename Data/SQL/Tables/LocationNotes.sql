/****** Object:  Table [LocationNotes]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [LocationNotes](
	[NoteId] [int] IDENTITY(1,1) NOT NULL,
	[LocationId] [int] NULL,
	[Note] [nvarchar](max) NULL,
	[CreatedOn] [datetime] NULL,
	[UserId] [int] NULL,
	[Pin] [bit] NULL,
	[IsDeleted] [bit] NULL,
	[ModifiedBy] [int] NULL,
	[ModifiedOn] [datetime] NULL,
 CONSTRAINT [PK_LocationNotes] PRIMARY KEY CLUSTERED 
(
	[NoteId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [LocationNotes] ADD  CONSTRAINT [DF_LocationNotes_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO
ALTER TABLE [LocationNotes] ADD  CONSTRAINT [DF_LocationNotes_Pin]  DEFAULT ((0)) FOR [Pin]
GO
ALTER TABLE [LocationNotes] ADD  CONSTRAINT [DF_LocationNotes_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
