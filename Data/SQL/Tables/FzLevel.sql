/****** Object:  Table [FzLevel]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [FzLevel](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DeviceId] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[Iteration] [int] NULL,
	[HeaderId] [int] NULL,
	[LocationId] [int] NULL,
	[l1] [float] NULL,
	[l2] [float] NULL,
	[l3] [float] NULL,
	[l4] [float] NULL,
	[l5] [float] NULL,
	[l6] [float] NULL,
	[l7] [float] NULL,
	[l8] [float] NULL,
	[l9] [float] NULL,
	[l10] [float] NULL,
	[l11] [float] NULL,
	[l12] [float] NULL,
	[l13] [float] NULL,
	[l14] [float] NULL,
	[l15] [float] NULL,
	[l16] [float] NULL,
	[l17] [float] NULL,
	[l18] [float] NULL,
	[l19] [float] NULL,
	[l20] [float] NULL,
	[l21] [float] NULL,
	[l22] [float] NULL,
	[l23] [float] NULL,
	[l24] [float] NULL,
	[l25] [float] NULL,
	[l26] [float] NULL,
	[l27] [float] NULL,
	[l28] [float] NULL,
	[l29] [float] NULL,
	[l30] [float] NULL,
	[l31] [float] NULL,
	[l32] [float] NULL,
	[l33] [float] NULL,
	[l34] [float] NULL,
	[l35] [float] NULL,
	[l36] [float] NULL,
	[l37] [float] NULL,
	[l38] [float] NULL,
	[l39] [float] NULL,
	[l40] [float] NULL,
	[l41] [float] NULL,
	[l42] [float] NULL,
	[l43] [float] NULL,
	[l44] [float] NULL,
	[l45] [float] NULL,
	[l46] [float] NULL,
	[l47] [float] NULL,
	[l48] [float] NULL,
	[l49] [float] NULL,
	[l50] [float] NULL,
	[l51] [float] NULL,
	[l52] [float] NULL,
	[l53] [float] NULL,
	[l54] [float] NULL,
	[l55] [float] NULL,
	[l56] [float] NULL,
	[l57] [float] NULL,
	[l58] [float] NULL,
	[l59] [float] NULL,
	[l60] [float] NULL,
	[l61] [float] NULL,
	[l62] [float] NULL,
	[l63] [float] NULL,
	[l64] [float] NULL,
	[l65] [float] NULL,
	[l66] [float] NULL,
	[l67] [float] NULL,
	[l68] [float] NULL,
	[l69] [float] NULL,
	[l70] [float] NULL,
	[l71] [float] NULL,
	[l72] [float] NULL,
	[l73] [float] NULL,
	[l74] [float] NULL,
	[l75] [float] NULL,
	[l76] [float] NULL,
	[l77] [float] NULL,
	[l78] [float] NULL,
	[l79] [float] NULL,
	[l80] [float] NULL,
	[l81] [float] NULL,
	[l82] [float] NULL,
	[l83] [float] NULL,
	[l84] [float] NULL,
	[l85] [float] NULL,
	[l86] [float] NULL,
	[l87] [float] NULL,
	[l88] [float] NULL,
	[l89] [float] NULL,
	[l90] [float] NULL,
	[AvgL] [float] NULL,
	[StDevL] [float] NULL,
	[IsDeleted] [bit] NOT NULL,
	[ModifiedOn] [datetime] NULL,
	[WaterHeight] [float] NULL,
	[WaterDischarge] [float] NULL,
	[CalibrationId] [int] NULL,
	[FilteredStDevL] [float] NULL,
 CONSTRAINT [PK_FzLevel] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IsDeleted_Index]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IsDeleted_Index] ON [FzLevel]
(
	[IsDeleted] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FzLevel_CreatedOn]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_FzLevel_CreatedOn] ON [FzLevel]
(
	[CreatedOn] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FzLevel_DeviceId]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_FzLevel_DeviceId] ON [FzLevel]
(
	[DeviceId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FzLevel_HeaderId]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_FzLevel_HeaderId] ON [FzLevel]
(
	[HeaderId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FzLevel_LocationId]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_FzLevel_LocationId] ON [FzLevel]
(
	[LocationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [nci_wi_FzLevel_9F2230FF52728ED077FE7C4C6313452B]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [nci_wi_FzLevel_9F2230FF52728ED077FE7C4C6313452B] ON [FzLevel]
(
	[Iteration] ASC,
	[ModifiedOn] ASC
)
INCLUDE([AvgL],[DeviceId],[HeaderId],[l1],[l10],[l2],[l3],[l4],[l5],[l6],[l7],[l8],[l9]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
ALTER TABLE [FzLevel] ADD  CONSTRAINT [DF_FzLever_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO
ALTER TABLE [FzLevel] ADD  CONSTRAINT [DF_FzLevel_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
