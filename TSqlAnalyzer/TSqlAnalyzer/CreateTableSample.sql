USE [Test]
GO

/****** Object:  Table [dbo].[User]    Script Date: 2023-11-22 ¿ÀÈÄ 8:14:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[User](
	[ID] [int] NULL DEFAULT 0,
	[Name] [nchar](10) NOT NULL,
	[Name2] [nchar](10) NOT NULL,
) ON [PRIMARY]

ALTER TABLE [dbo].[User] ADD CONSTRAINT [DF_User] DEFAULT '' FOR [Name];
GO