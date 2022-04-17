CREATE TABLE [NoaaForecastData]
(
    [ForecastId] [int] NOT NULL,
    [Timestamp] [datetime] NOT NULL,
    [Stage] [float] NULL,
    [Discharge] [float] NULL
) ON [PRIMARY]
GO

ALTER TABLE [NoaaForecastData]  WITH CHECK ADD  CONSTRAINT [FK_NoaaForecastData_NoaaForecasts] FOREIGN KEY([ForecastId])
REFERENCES [NoaaForecasts] ([ForecastId])
GO


CREATE NONCLUSTERED INDEX [IX_NoaaForecastData] ON [NoaaForecastData]
(
    [ForecastId] ASC,
    [Timestamp] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]

GO
