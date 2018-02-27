/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script.	
 Use SQLCMD syntax to include a file in the pre-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
--------------------------------------------------------------------------------------
--Create schema if not exists
--------------------------------------------------------------------------------------
IF NOT EXISTS
(
    SELECT schema_name
    FROM information_schema.schemata
    WHERE schema_name = 'IOHELPER'
)
    BEGIN
        EXEC sp_executesql
             N'CREATE SCHEMA IOHELPER';
    END;
--------------------------------------------------------------------------------------
--Create asymetric key 
--
--Replace the path 'D:\Cloud\DataBaseHelper\DBHelper\IOHelper\' with your path
--
--------------------------------------------------------------------------------------
    IF
(
    SELECT COUNT(*)
    FROM master.sys.asymmetric_keys
    WHERE name = 'askIoHelper'
) = 0
            BEGIN
                EXEC sp_executesql
N'USE MASTER;
CREATE ASYMMETRIC KEY [askIoHelper]
FROM FILE = ''D:\Cloud\DataBaseHelper\DBHelper\IOHelper\IoHelper.snk''
ENCRYPTION BY PASSWORD = ''SimpleTalk''';
            END;
        IF NOT EXISTS
(
    SELECT loginname
    FROM master.dbo.syslogins
    WHERE name = 'loginIoHelper'
)
            BEGIN
                DECLARE @sqlStatement AS NVARCHAR(1000);
                SELECT @SqlStatement = 'CREATE LOGIN [loginIoHelper] 
FROM ASYMMETRIC KEY askIoHelper';
                EXEC sp_executesql
                     @SqlStatement;
                EXEC sp_executesql
N'USE MASTER;
GRANT UNSAFE ASSEMBLY TO [loginIoHelper];';
            END;
