SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Map_MapMetadata]
(
    Id int IDENTITY(1,1) NOT NULL,
    RegionId int not null,
    RefererList varchar(500),
    SlugList varchar(500),
    TileDescJson TEXT NOT NULL,
    WebStyles TEXT NOT NULL,
    MobileStyles TEXT NOT NULL,
    CONSTRAINT [PK_Map_MapMetadata] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [Map_MapMetadata]  WITH CHECK ADD  CONSTRAINT [FK_MapMetadata_Region] FOREIGN KEY([RegionId])
REFERENCES [Regions] ([RegionId])
GO
