## DBHelper	

@octocat :+1: This PR looks great - it's ready to merge! :shipit:

SMO library which has, as a result, a DLL. In the library, you can find a lot of methods to work with SQL Server database.


Method description                               | Method signature
-------------------------------------------------|---------------------------------------------------------------------------------
To take a copy only backup                       | BackupDatabase(ServerConnection,ILog,ref string, bool)
To perform database check                        | CheckDb(ServerConnection, ILog , ref string )
To put database in user access mode              | PutDbInUserMode(ServerConnection, DatabaseUserAccess ,bool, ILog, ref string)
To kill all user process for particular database | KillAllProcessesForOneDatabase(ServerConnection, ref string)
To determine the log size                        | DetermineLogSize(ServerConnection,ref string)
To set database in particular recovery mode      | SetRecoveryMode(ServerConnection,RecoveryModel,bool, ILog , ref string)
To restore the database                          | RestoreDatabase(ServerConnection, ILog, ref string)
To shrink the database log                       | ShrinkLog(ServerConnection,int,ILog,ref string)
To determine is the database online              | IsTheDataBaseOnLine(ServerConnection, ILog, ref string)


## IOHeLper	

SQLCLR project which has, as a result, a DLL that should be published on the database server. 

Method description                 | Method signature
---------------------------------- |--------------------------------------------------------------------------------------------
Can I perform backup?              | CanIPerformABackup(ServerConnection, ILog , ref string )
To delete old backup files         | DeleteBackupFiles(ServerConnection, ILog, ref string)
To determine number and size       | DetermineNumberOfBackupFiles(ServerConnection, ILog, ref int, ref Int64,ref string)

 
## Tester	

A console application that could be useful to make some tests.

## SmoIntroduction	

A console application that was our first example
