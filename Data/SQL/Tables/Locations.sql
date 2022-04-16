/****** Object:  Table [Locations]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Locations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LocationName] [varchar](100) NOT NULL,
    [ShortName][nvarchar](50) NULL,
	[RegionId] [int] NOT NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[IsActive] [bit] NOT NULL,
	[IsPublic] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[IsOffline] [bit] NOT NULL,
	[Rank] [float] NULL,
	[PublicLocationId] [varchar](50) NULL,

    -- This is stored in inches above sea level, as surveyed
	[BenchmarkElevation] [float] NULL,

    -- These fields are stored in inches relative to the benchmark (positive == above benchmark, negative == below)
	[GroundHeight] [float] NOT NULL,
	[Green] [float] NULL,                   -- flood stage marker (below green == no flood)
	[Brown] [float] NULL,                   -- flood stage marker (between green and brown == moderate, above brown == flooding)
	[YMin] [float] NULL,                    -- minimum (bottom) value for chart Y axis
	[YMax] [float] NULL,                    -- maximum (top) value for chart Y axis
	[DischargeMin] [float] NULL,            -- minimum (bottom) value for discharge chart Y axis
	[DischargeMax] [float] NULL,            -- maximum (top) value for discharge chart Y axis
    [DischargeStageOne] [float] NULL,       -- flood stage marker 1 for discharge chart
    [DischargeStageTwo] [float] NULL,       -- flood stage marker 1 for discharge chart
	[RelativeSensorHeight] [float] NULL,
	[RoadSaddleHeight] [float] NULL,
	[MarkerOneHeight] [float] NULL,
	[MarkerTwoHeight] [float] NULL,

    -- This is in feet per minute
    [MaxChangeThreshold] [float] NULL,

    -- These fields are purely descriptive/cosmetic.
	[Description] [varchar](300) NULL,
	[Address] [nvarchar](max) NULL,
	[GeoData] [geography] NULL,
	[NearPlaces] [nvarchar](max) NULL,
	[Reason] [nvarchar](max) NULL,
	[ContactInfo] [nvarchar](max) NULL,
	[BenchmarkIsProvisional] [bit] NOT NULL,
	[BenchmarkDescription] [varchar](300) NULL,
	[RoadDisplayName] [varchar](300) NULL,
	[MarkerOneDescription] [varchar](300) NULL,
	[MarkerTwoDescription] [varchar](300) NULL,

    -- These fields are unused.
	[TimeZone] [varchar](100) NOT NULL,
	[Yellow] [float] NULL,
	[LocationUpdateMinutes] [int] NULL,
	[SeaLevel] [float] NULL,
	[LocationNumber] [varchar](50) NULL,    -- obsolete (replaced by PublicLocationId)

 CONSTRAINT [PK__Location__3214EC07CF72BBA0] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [Locations] ADD  CONSTRAINT [DF__Locations__Groun__17036CC0]  DEFAULT ((0)) FOR [GroundHeight]
GO
ALTER TABLE [Locations] ADD  CONSTRAINT [DF__Locations__IsAct__236943A5]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [Locations] ADD  CONSTRAINT [DF__Locations__IsPub__245D67DE]  DEFAULT ((0)) FOR [IsPublic]
GO
ALTER TABLE [Locations] ADD  CONSTRAINT [DF_Locations_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [Locations] ADD  CONSTRAINT [DF__Locations__IsOff__324172E1]  DEFAULT ('0') FOR [IsOffline]
GO
ALTER TABLE [Locations] ADD  DEFAULT ((0)) FOR [BenchmarkIsProvisional]
GO
ALTER TABLE [Locations]  WITH CHECK ADD  CONSTRAINT [FK_Locations_Regions] FOREIGN KEY([RegionId])
REFERENCES [Regions] ([RegionId])
GO
ALTER TABLE [Locations] CHECK CONSTRAINT [FK_Locations_Regions]
GO
