SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Map_MapGrid]
(
    Id int IDENTITY(1,1) NOT NULL,
    RegionId int not null,
    Zoom int not null,
    GridColumn int not null,
    GridRow int not null,
    TileId varchar(32) not null
    CONSTRAINT [PK_Map_MapGrid] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Map_MapGrid] ON [Map_MapGrid]
(
    RegionId ASC,
    Zoom ASC,
    GridColumn ASC,
    GridRow ASC,
    TileId ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO

ALTER TABLE [Map_MapGrid]  WITH CHECK ADD  CONSTRAINT [FK_MapGrid_Region] FOREIGN KEY([RegionId])
REFERENCES [Regions] ([RegionId])
GO
