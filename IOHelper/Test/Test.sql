USE [AdventureWorks2014]
GO

DECLARE	@return_value int,
		@freeSpace float,
		@errorMessage nvarchar(1000)
--
--==Determine free space on drive "C"  in MB  ==--
--
EXEC	@return_value = [IOHELPER].[FreeSpace] @driveLetter=N'C',
		@freeSpace = @freeSpace OUTPUT,
		@errorMessage = @errorMessage OUTPUT

SELECT	@freeSpace as N'@freeSpace',
		@errorMessage as N'@errorMessage'

SELECT	'Return Value' = @return_value

GO

DECLARE	@return_value int,
		@freeSpace float,
		@errorMessage nvarchar(1000)

--
--==Determine free space on drive "C"  in GB  ==--
--
EXEC	@return_value = [IOHELPER].[FreeSpace] @driveLetter=N'C',
        @unitOfMeasure = 3,
		@freeSpace = @freeSpace OUTPUT,
		@errorMessage = @errorMessage OUTPUT

SELECT	@freeSpace as N'@freeSpace',
		@errorMessage as N'@errorMessage'

SELECT	'Return Value' = @return_value

GO


--
--Delete files on C:\TMP directory with filter "FILE_TO_DELETE*.TXT"
--

DECLARE	@return_value int,
		@errorMessage nvarchar(1000)

EXEC @return_value = [IOHELPER].[DeleteFiles] @path = N'C:\TMP\'
									,@filter = N'FILE_TO_DELETE*.TXT'
									,@errorMessage = @errorMessage OUTPUT;

SELECT	@errorMessage as N'@errorMessage'

SELECT	'Return Value' = @return_value

GO
