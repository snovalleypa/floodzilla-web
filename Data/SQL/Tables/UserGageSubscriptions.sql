SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE UserGageSubscriptions
(
	UserId int NOT NULL,
    LocationId int NOT NULL,
)
GO

ALTER TABLE UserGageSubscriptions  WITH CHECK ADD  CONSTRAINT FK_UserGageSubscriptions_Users FOREIGN KEY(UserId)
  REFERENCES Users (Id)
GO

ALTER TABLE UserGageSubscriptions  WITH CHECK ADD  CONSTRAINT FK_UserGageSubscriptions_Locations FOREIGN KEY(LocationId)
  REFERENCES Locations (Id)
GO

