SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [PushReceiptLog]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
    [Timestamp] datetime not null,
	[MachineName] [varchar](50) not null,
    [LogEntryType] varchar(50) not null,
    [TicketIds] text not null,
    [Result] varchar(50) null,
    [Response] text null
) ON [PRIMARY]
GO

