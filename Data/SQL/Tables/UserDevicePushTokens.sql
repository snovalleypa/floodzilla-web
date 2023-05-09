SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [UserDevicePushTokens]
(
	[Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [DeviceId] VARCHAR(256) NULL,
    [Platform] VARCHAR(64) NOT NULL,
    [Language] VARCHAR(64) NULL,
    [Token] VARCHAR(4096) NOT NULL,
    [LastRefreshTime] DATETIME NOT NULL,
    [LastAttemptTime] DATETIME NULL,
    [LastSuccessTime] DATETIME NULL,
    [FailureCount] INT NULL
) ON [PRIMARY]
GO

ALTER TABLE UserDevicePushTokens
  ADD CONSTRAINT FK_UserDevicePushTokens_Users 
    FOREIGN KEY (UserId)
    REFERENCES Users (Id)
GO

