/****** Object:  Table [DeviceTypes]    Script Date: 12/20/2019 11:32:51 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [DeviceTypes](
	[DeviceTypeId] [int] NOT NULL,
	[DeviceTypeName] [nvarchar](50) NULL,
 CONSTRAINT [PK_DeviceTypes] PRIMARY KEY CLUSTERED 
(
	[DeviceTypeId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
