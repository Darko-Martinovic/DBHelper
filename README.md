The code for the Simple Talk article [Programming SQL Server with SQL Server Management Objects Framework](https://www.red-gate.com/simple-talk/dotnet/c-programming/programming-sql-server-sql-server-management-objects-framework/), brought up to date: .NET 10, SMO 181 on Microsoft.Data.SqlClient, and SQL Server 2017 to 2025 on Windows or Linux. The code as the article published it is the tag [`v1-article`](https://github.com/Darko-Martinovic/DBHelper/tree/v1-article).

## Getting started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a SQL Server with the AdventureWorks sample database, which the examples are written against.

1. **Start SQL Server.** Any SQL Server 2017 or later, or the container in `compose.yaml`:

   ```bash
   MSSQL_SA_PASSWORD='<a strong password>' docker compose up -d     # or: podman compose up -d
   ```

2. **Restore AdventureWorks** as `AdventureWorks`. For the container, download the backup into `.adventureworks`, which the container mounts:

   ```bash
   mkdir -p .adventureworks
   curl -L -o .adventureworks/AdventureWorks2017.bak https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2017.bak
   docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -C -U sa -P "$MSSQL_SA_PASSWORD" -Q "RESTORE DATABASE AdventureWorks FROM DISK = N'/var/opt/mssql/backup/AdventureWorks2017.bak' WITH MOVE N'AdventureWorks2017' TO N'/var/opt/mssql/data/AdventureWorks.mdf', MOVE N'AdventureWorks2017_log' TO N'/var/opt/mssql/data/AdventureWorks_log.ldf'"
   ```

3. **Point the examples at it.** `SmoIntroduction/appsettings.json` connects to the default instance on this machine with Windows authentication. For the container, or anything else, keep the connection string in user secrets:

   ```bash
   dotnet user-secrets --project SmoIntroduction set "ConnectionStrings:ConnStr" "Server=localhost,1433;Database=AdventureWorks;User ID=sa;Password=<the password>;TrustServerCertificate=True;Application Name=SmoIntroduction"
   dotnet user-secrets --project SmoIntroduction set "Examples:KeyPassword" "<a strong password>"
   ```

   `Examples:KeyPassword` protects master's database master key, when `Tde` or the DBHelper tour has to create it, and the private keys of the certificates they export. Environment variables work too: `DBHELPER_ConnectionStrings__ConnStr`, `DBHELPER_Examples__KeyPassword`.

4. **Run the examples:**

   ```bash
   dotnet run --project SmoIntroduction               # pick from a menu
   dotnet run --project SmoIntroduction -- CreateGraph # run one
   ```

The examples change AdventureWorks (`CreateSysVerTempTables` makes `Person.EmailAddress` temporal, for example) and create and drop the databases `TestPartition` and `CustRecords`. Run them on a server you can throw away.

## Tests

```bash
dotnet test --solution DBHelper.slnx
```

The tests start SQL Server in a container with [Testcontainers](https://dotnet.testcontainers.org/), so they need Docker or Podman, restore AdventureWorks into it (the backup is downloaded once, to `ADVENTUREWORKS_CACHE` or the temp folder) and run every example and every DBHelper method. They use SQL Server 2025 unless `MSSQL_IMAGE` names another image; CI runs them on 2022 and 2025. With Podman, set `TESTCONTAINERS_RYUK_DISABLED=true`.

## :white_check_mark: DBHelper

An SMO library of maintenance tasks on the database a `ServerConnection` points at. Each method connects if the connection is closed and disconnects when it is done; a connection that is already open stays open. Failures are thrown: SMO's exceptions keep SQL Server's message in their inner exceptions, which `ex.AllMessages()` collects. Each method takes an optional `ILogger` for its progress.

Method | Description
---|---
`BackupDatabase(cnn, encryption, verify, useCompression, copyOnly)` | Backs up the database to a new file in the server's backup directory and returns its path. A copy-only, verified, compressed backup by default; encrypted when asked, and never silently unencrypted
`RestoreDatabase(cnn, backupFile)` | Restores the database over itself, from a file or its newest dbHelper backup in msdb's backup history
`LastBackupFile(cnn)` | The newest dbHelper backup of the database, from msdb
`IsThereAnyBackupTaken(cnn)` | Is there a dbHelper backup, and is its file still there?
`DetermineNumberOfBackupFiles(cnn)` | The database's dbHelper backup files, newest first, and their total size
`DeleteBackupFiles(cnn, keepLatest)` | Deletes the database's dbHelper backup files but the newest ones (one by default)
`CanIPerformABackup(cnn, unit)` | Is there room for a backup on the drive of the backup directory?
`DetermineFreeSpaceForBackups(cnn, unit)` | The free space on the drive of the backup directory
`DetermineSize(sizeInMb, unit)` | Converts a size in MB to another unit
`CheckDb(cnn, repairAllowDataLoss)` | DBCC CHECKALLOC, CHECKCATALOG and CHECKDB; returns what they report. Repairs only when asked
`PutDbInUserMode(cnn, mode, killUserProcess)` | Single, restricted or multiple user access
`KillAllProcessesForOneDatabase(cnn)` | Kills the other sessions in the database
`SetRecoveryMode(cnn, model, killUserProcess)` | Sets the recovery model
`DetermineLogSize(cnn)` | The size of the log files, in MB
`ShrinkLog(cnn, targetMb)` | Shrinks the log towards a target size
`ForceShrinkingLog(cnn, targetMb)` | Shrinks the log of a database in the full recovery model through the simple model, then takes the full backup that starts the log chain again
`IsTheDataBaseOnLine(cnn)` | Is the database online?
`BackupCertificateExists(cnn, name)`, `MasterKeyExists(cnn)` | Is there a certificate or a database master key in master?
`CreateBackupCertificate(cnn, masterKeyPassword, name, certificateFile, privateKeyFile, privateKeyPassword)` | Creates a certificate in master for encrypted backups, and the master key if there isn't one, and exports them. It never drops an existing key or certificate

The IOHelper SQLCLR assembly the article used for the free space and the backup files is gone, so nothing has to be deployed to the server any more and the library works on Linux too. In its place, the library reads `sys.dm_os_enumerate_fixed_drives` for the free space and `sys.dm_os_enumerate_filesystem` for the backup files, and deletes old backups with `xp_delete_file`, which deletes only SQL Server backup files. The last two are undocumented, though SQL Server's own maintenance plans use `xp_delete_file`; the tests check all three on SQL Server 2022 and 2025.

## :white_check_mark: SmoIntroduction

The article's examples, in one console with a menu. The DBHelper tour runs the library's tasks one after another, as the old Tester console did, on a database of its own (`Examples:TourDatabase`, `DbHelperDemo` by default), since the restores replace it.

Class | Description
---|---
CreateGraph | Shows how to create NODE & EDGE tables
CreateMoTable | Shows how to create the memory optimized tables
CreatePartitionScheme | Shows how to create the partition scheme and the partition function
CreateStoredProc | Shows how to create the stored procedure
CreateSysVerTempTables | Shows how to create system-versioned ( temporal ) tables
CreateTable | Shows how to create traditional - disk based tables
ExtProperties | Shows how to manipulate with extended properties
ExtendedEvents | Shows how to manipulate with extended events ( create session, add targets, add events )
ListAllUserDefinedDataTypes | Shows how to get and recreate all user-defined data type in a database
RebuildIndexes | Shows how to rebuild indexes and update statistics
ServerConfiguration | Shows how to get the server configuration
ServerEvents | Shows how to subscribe to server events
SetDefaultInitField | Shows how to reduce the amount of T-SQL needed for object initialisation
Tde | Shows how to encrypt the database
DbHelperTour | Runs the DBHelper library's tasks

Where the examples opened a script in Notepad, they now print it. `ServerEvents` needs Windows on both sides: SMO receives server events through WMI, so it works only from a Windows client against SQL Server on Windows.

## Changes since the article

- .NET 10 and SMO 181 instead of .NET Framework 4.8 and three different SMO versions (the old solution only built where SQL Server Management Studio had put SMO in the GAC). Microsoft.Data.SqlClient replaces System.Data.SqlClient.
- One console with a menu replaces the fourteen `Main` methods chosen with `<StartupObject>`, and the Tester console. The settings moved from `App.config` to `appsettings.json` and user secrets; the password that was in the code is gone.
- `CreateBackupCertificate` and `Tde` dropped master's database master key and an existing certificate before creating new ones, which loses every key the master key protects. They now create what is missing and keep what is there.
- `CheckDb` repaired with `REPAIR_ALLOW_DATA_LOSS` in single-user mode every time. It now only checks, unless asked to repair.
- `ForceShrinkingLog` ended with a copy-only backup, which doesn't start the log chain again after the switch back to the full recovery model, so log backups failed until the next full backup. It now takes a full backup.
- `SetRecoveryMode` didn't change the recovery model unless it also killed the other sessions. `ShrinkLog` used `TRUNCATEONLY`, which ignores the target size. `DetermineSize` was off by a factor of 1024 for kilobytes. `IsThereAnyBackupTaken` was true even when the backup file was gone.
- The last backup's file name is read from msdb's backup history instead of an extended property the library wrote on the user database.
- The library throws instead of returning `false` with the message in a `ref string`, and logs through `ILogger`.
- The IOHelper SQLCLR assembly is gone (see DBHelper above). It was an UNSAFE assembly signed with a key published in the repository, and any caller of its `DeleteFiles` procedure could delete any file SQL Server's service account could reach.
- Paths on the server use the server's separator, so the examples work on SQL Server on Linux.

## License

[MIT](LICENSE)
