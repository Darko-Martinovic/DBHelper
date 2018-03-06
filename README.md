## DBHelper	

SMO library which has, as a result, a DLL. In the library, you can find a lot of methods to work with SQL Server database.


Method description                 | Method signature
---------------------------------- |--------------------------------------------------------------------------------------------
To take a copy only backup         | BackupDatabase(ServerConnection,ILog,ref string, bool)
To perform database check          | CheckDb(ServerConnection, ILog , ref string )
To put database in user access mode| PutDbInUserMode(ServerConnection, DatabaseUserAccess ,bool, ILog, ref string)
To kill all user process for       | KillAllProcessesForOneDatabase(ServerConnection, ref string)
particular database                |
To determine the log size          | DetermineLogSize(ServerConnection,ref string)
To set database in particular      | SetRecoveryMode(ServerConnection,RecoveryModel,bool, ILog , ref string)
recovery mode                      |
To restore database                | RestoreDatabase(ServerConnection, ILog, ref string)
To shrink the database log         | ShrinkLog(ServerConnection,int,ILog,ref string)
To determine is the database online| IsTheDataBaseOnLine(ServerConnection, ILog, ref string)


## IOHeLper	

SQLCLR project which has, as a result, a DLL that could be published on the database server. 

The resulting assembly contains methods to determine free disk space on the particular drive, 

list files in a particular directory and delete a file in particular directory.

// Can I perform backup? Is there enough disk space

 public static bool CanIPerformABackup(ServerConnection cnn, ILog logger, ref string errMessage)
 
 // To delete old backup files
 
  public static bool DeleteBackupFiles(ServerConnection cnn, ILog logger, ref string errMessage)
  
  // To determine number and size of backup files
  
public static bool DetermineNumberOfBackupFiles( ServerConnection cnn, ILog logger, ref int numberOfFiles, ref Int64 totalSizeInBytes,
                                    ref string errorMessage)
  
## Tester	

A console application that could be useful to make some tests.

## SmoIntroduction	

A console application that was our first example
