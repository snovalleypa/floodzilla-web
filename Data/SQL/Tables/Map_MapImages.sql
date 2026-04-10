SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Map_MapImages]
(
    Id int IDENTITY(1,1) NOT NULL,
    RegionId int not null,
    TileId varchar(32) not null,
    Hash varchar(32) not null,
    TileData varbinary(max) not null
    CONSTRAINT [PK_Map_MapImages] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Map_MapImages_RegionIdTileId] ON [Map_MapImages]
(
    [RegionId] ASC,
    [TileId] ASC
)
GO

ALTER TABLE [Map_MapImages] WITH CHECK ADD  CONSTRAINT [FK_MapImages_Region] FOREIGN KEY([RegionId])
REFERENCES [Regions] ([RegionId])
GO

