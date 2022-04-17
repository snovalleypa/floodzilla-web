/****** Object:  Table [HydroObserved]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HydroObserved](
	[ObserveId] [int] IDENTITY(1,1) NOT NULL,
	[ObservedOn] [datetime] NULL,
	[FetchId] [int] NULL,
	[Flow] [float] NULL,
	[Stage] [float] NULL,
 CONSTRAINT [PK_HydroObserved] PRIMARY KEY CLUSTERED 
(
	[ObserveId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_HydroObserved]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_HydroObserved] ON [HydroObserved]
(
	[FetchId] DESC,
	[ObservedOn] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
ALTER TABLE [HydroObserved] ADD  CONSTRAINT [DF_HydroObserved_ObservedOn]  DEFAULT (getutcdate()) FOR [ObservedOn]
GO
