SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FfmpegValidationRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Filenames] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
