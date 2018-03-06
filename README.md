## DBHelper	

SMO library which has, as a result, a DLL. In the library, you can find a lot of methods to work with SQL Server database.


Method description                 | Method signature
---------------------------------- |--------------------------------------------------------------------------------------------
To take a copy only backup         | BackupDatabase(ServerConnection,ILog,ref string, bool)
To perform database check          | CheckDb(ServerConnection, ILog , ref string )
To put database in user access mode| PutDbInUserMode(ServerConnection, DatabaseUserAccess ,bool, ILog, ref string)







// To kill all user process for particular database

 public static bool KillAllProcessesForOneDatabase(ServerConnection cnn, ref string errorLog)

// To determine the log size

public static double DetermineLogSize(ServerConnection cnn, ref string errorLog)
 
 // To set database in particular recovery mode
 
public static bool SetRecoveryMode(ServerConnection cnn,RecoveryModel dbMode,bool killUserProcess, ILog logger, ref string errorMessage)

// To restore database

public static bool RestoreDatabase(ServerConnection cnn, ILog logger, ref string errMessage)

// To shrink the database log

public static bool ShrinkLog(ServerConnection cnn,  int TargetLogFileSizeMB,ILog logger,  ref string errMessage)

// To determine is the database online

public static bool IsTheDataBaseOnLine(ServerConnection cnn, ILog logger, ref string errMessage )


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
