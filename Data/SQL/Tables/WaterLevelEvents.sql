CREATE TABLE [WaterLevelEvents]
(
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [LocationId] [int] NOT NULL,
    [WaterHeight] [float] NULL, -- feet ASL
    [WaterDischarge] [float] NULL, -- cfs
    [EventType] [varchar](100) NOT NULL,
    [Timestamp] [datetime] NOT NULL,
    [Observed] [bit] NOT NULL,
    [ApproximateTime] [bit] NOT NULL,
    [Source] [varchar](100) NOT NULL,

    CONSTRAINT [PK_WaterLevelEvents] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
GO


ALTER TABLE WaterLevelEvents 
  ADD CONSTRAINT FK_WaterLevelEvents_Locations 
    FOREIGN KEY (LocationId)
    REFERENCES Locations (Id)
GO

