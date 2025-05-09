/****** Object:  Table [SensorReadings]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SensorReadings](

    -- NOTE: In the current production database, this column was created after the
    -- fact, so it's not linearly increasing with time for old data the way you'd 
    -- expect IDENTITY columns to be...
    -- Also, USGS readings are inserted after some delay, so they'll be out of IDENTITY order as well.
    [Id] [int] IDENTITY(1,1) NOT NULL,

    [LocationId] [int] NULL,
    [DeviceId] [int] NULL,
    [DeviceTypeId] int NULL, 

    [Timestamp] [datetime] NULL,            -- Time the reading was received
    [DeviceTimestamp] [datetime] NULL,      -- Time the device made the reading, if available
    [BatteryVolt] [int] NULL,               -- [deprecated] Battery level in millivolts (blame DaveS for the bad name)
    [BatteryPercent] [float] NULL,          -- Battery percentage level; supercedes BatteryVolt

    -- Currently, these distances/heights are in inches.
    -- //$ TODO: Convert everything to feet
    [DistanceReading] [float] NULL,         -- The raw distance reading from the sensor
    [RawWaterHeight] [float] NULL,          -- The calculated water height (sensor height - distance reading) (inches above sea level), unclipped
    [WaterHeight] [float] NULL,             -- The final displayable water height (possibly clipped to ground) (inches above sea level)
    [GroundHeight] [float] NULL,            -- Ground height as defined by the location (inches above sea level)

    -- These are the same as above, but in feet; this is temporary, and eventually
    -- everything will just be stored in feet.
    [DistanceReadingFeet] [float] NULL,     -- The raw distance reading from the sensor
    [RawWaterHeightFeet] [float] NULL,      -- The calculated water height (sensor height - distance reading) (feet above sea level), unclipped
    [WaterHeightFeet] [float] NULL,         -- The final displayable water height (possibly clipped to ground) (feet above sea level)
    [GroundHeightFeet] [float] NULL,        -- Ground height as defined by the location (feet above sea level)

    [WaterDischarge] [float] NULL,          -- Flow in CFS, if provided
    [CalcWaterDischarge] [float] NULL,      -- Calculated Flow in CFS based on gage height, if available

    -- Snapshotted from the location.  Feet above sea level
    [BenchmarkElevation] [float] NULL,

    -- Snapshotted values from the location, all in feet relative to benchmark
    [Green] [float] NULL,                   -- flood stage marker (below green == no flood)
    [Brown] [float] NULL,                   -- flood stage marker (between green and brown == moderate, above brown == flooding)
    [RelativeSensorHeight] [float] NULL,
    [RoadSaddleHeight] [float] NULL,
    [MarkerOneHeight] [float] NULL,
    [MarkerTwoHeight] [float] NULL,

    -- For sensors that send data via radio (e.g. Senix LORA sensors)
    [RSSI] [float] NULL,
    [SNR] [float] NULL,

    -- //$ TODO: Convert to bit
    [IsDeleted] [int] NOT NULL,

    [IsFiltered] [bit] NOT NULL,            -- TRUE if this reading was discarded for being out of range; IsDeleted should also be TRUE

    -- This is used to tag readings with metadata about the listener version, etc, for debugging purposes.
    [ListenerInfo] [varchar](200) NULL,

    -- If a reading has been deleted/undeleted, this is the last reason why
    [DeleteReason] [nvarchar](200) NULL,

    -- The raw data as provided by the sensor; format and contents vary according to the sensor type
    [RawSensorData] [text] NULL,

 CONSTRAINT [PK_SensorReadings] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [IX_SensorReadings]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_SensorReadings] ON [SensorReadings]
(
    [LocationId] ASC,
    [IsDeleted] ASC,
    [Timestamp] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SensorReadings_Device]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_SensorReadings_Device] ON [SensorReadings]
(
    [DeviceId] ASC,
    [IsDeleted] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SensorReadings_Location]    Script Date: 12/20/2019 11:32:51 AM ******/
CREATE NONCLUSTERED INDEX [IX_SensorReadings_Location] ON [SensorReadings]
(
    [LocationId] ASC,
    [IsDeleted] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [Ix_Sensorreadings_Minimal] ON [SensorReadings]
(
    [LocationId] ASC,
    [IsDeleted] ASC,
    [Timestamp] DESC,
    [WaterHeightFeet],
    [WaterDischarge]
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
