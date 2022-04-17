-- NOTE: Date is in UTC, but should correspond to beginning-of-day in Region time for the corresponding gage.

CREATE TABLE GageStatistics (
    [LocationId] [int] NOT NULL,
    [Date] [datetime] NOT NULL,
    [AverageBatteryMillivolts] [int] NULL,
    [PercentReadingsReceived] [float] NULL,
    [AverageRssi] [float] NULL,
    [SensorUpdateInterval] [int] NULL,
    [SensorUpdateIntervalChanged] [bit] NULL,
) ON [PRIMARY]
GO

