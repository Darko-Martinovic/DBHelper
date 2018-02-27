/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
ALTER PROCEDURE [dbo].[FreeSpace]
	@driveLetter [nchar](1),
	@unitOfMeasure [smallint]=2,
	@freeSpace [float] OUT,
	@errorMessage [nvarchar](1000) OUT 
	AS EXTERNAL NAME [SimpleTalk.SQLCLR.IOHelper].[StoredProcedures].[FreeSpace];
GO

IF EXISTS ( SELECT * 
            FROM   sysobjects 
            WHERE  id = object_id(N'[IOHELPER].[FreeSpace]') 
                   and OBJECTPROPERTY(id, N'IsProcedure') = 1 )
BEGIN
    DROP PROCEDURE [IOHELPER].[FreeSpace]
END

ALTER SCHEMA [IOHELPER] TRANSFER [dbo].[FreeSpace];
GO



ALTER PROCEDURE [dbo].[DeleteFiles]
	@path [nvarchar](1000),
	@filter [nvarchar](1000)=NULL,
	@errorMessage [nvarchar](1000) OUT 
	AS EXTERNAL NAME [SimpleTalk.SQLCLR.IOHelper].[StoredProcedures].[DeleteFiles]
GO

IF EXISTS ( SELECT * 
            FROM   sysobjects 
            WHERE  id = object_id(N'[IOHELPER].[DeleteFiles]') 
                   and OBJECTPROPERTY(id, N'IsProcedure') = 1 )
BEGIN
    DROP PROCEDURE [IOHELPER].[DeleteFiles]
END

ALTER SCHEMA [IOHELPER] TRANSFER [dbo].[DeleteFiles];


--Transfer function
IF EXISTS ( SELECT * 
			FROM   sysobjects 
			WHERE  id = object_id(N'[IOHELPER].[FileHelper]') 
				   and type = N'FT' )
BEGIN
	DROP FUNCTION [IOHELPER].[FileHelper]
END

ALTER SCHEMA  [IOHELPER] TRANSFER dbo.FileHelper;


