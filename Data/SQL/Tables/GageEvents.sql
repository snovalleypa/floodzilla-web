SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE GageEvents
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [LocationId] INT NOT NULL,
    [EventType] varchar(100) NOT NULL,
    [EventTime] [datetime] NOT NULL,
    [NotifierProcessedTime] [datetime] NULL,
    [NotificationResult] varchar(100) NULL,
	[EventDetails] [text] NULL,
)
GO

ALTER TABLE GageEvents 
  ADD CONSTRAINT FK_GageEvents_Locations 
    FOREIGN KEY (LocationId)
    REFERENCES Locations (Id)
GO

