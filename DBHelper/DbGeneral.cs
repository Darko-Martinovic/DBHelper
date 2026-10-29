using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DBHelper;

/// <summary>A size on disk, in the unit a caller asks for.</summary>
public enum DiskSizeUnit
{
    /// <summary>Bytes.</summary>
    Bytes = 0,
    /// <summary>Kilobytes (1024 bytes).</summary>
    KiloBytes = 1,
    /// <summary>Megabytes (1024 KB).</summary>
    MegaBytes = 2,
    /// <summary>Gigabytes (1024 MB).</summary>
    GigaBytes = 3,
    /// <summary>Terabytes (1024 GB).</summary>
    TeraBytes = 4
}

/// <summary>The dbHelper backup files of a database, newest first.</summary>
/// <param name="Paths">The full paths of the files, as the server sees them.</param>
/// <param name="TotalSizeInBytes">Their size together.</param>
public sealed record BackupFiles(IReadOnlyList<string> Paths, long TotalSizeInBytes)
{
    /// <summary>How many files there are.</summary>
    public int Count => Paths.Count;
}

/// <summary>The size of a log file before and after it was shrunk, in MB.</summary>
public sealed record LogShrinkResult(double BeforeMb, double AfterMb)
{
    /// <summary>How much the log file shrank, in MB.</summary>
    public double ReducedMb => BeforeMb - AfterMb;
}

/// <summary>
/// Maintenance tasks on the database a <see cref="ServerConnection"/> points at.
/// Each method connects if the connection is closed and disconnects when it is done.
/// Failures are thrown: SMO's own exceptions keep the server's message in their inner exceptions,
/// which <see cref="ExceptionExtensions.AllMessages"/> collects.
/// </summary>
public static class DbGeneral
{
    private const string BackupFilePrefix = "dbHelper_";

    #region Certificates

    /// <summary>Is there a certificate with this name in master, to encrypt backups with?</summary>
    public static bool BackupCertificateExists(ServerConnection cnn, string certificateName) =>
        Use(cnn, server => server.Databases["master"].Certificates[certificateName] != null);

    /// <summary>Does master have a database master key?</summary>
    public static bool MasterKeyExists(ServerConnection cnn) =>
        Use(cnn, server => server.Databases["master"].MasterKey != null);

    /// <summary>
    /// Creates a certificate in master to encrypt backups with, and master's database master key if
    /// there isn't one yet. An existing master key or certificate is never dropped: backups and
    /// databases encrypted with them couldn't be restored without them.
    /// </summary>
    /// <param name="cnn">The connection.</param>
    /// <param name="masterKeyPassword">The password for a new master key; unused when master already has one.</param>
    /// <param name="certificateName">The name of the new certificate.</param>
    /// <param name="certificateFile">Where to export the certificate, on the server's file system; null not to export it.</param>
    /// <param name="privateKeyFile">Where to export its private key, on the server's file system.</param>
    /// <param name="privateKeyPassword">The password that protects the exported private key.</param>
    /// <param name="logger">Where to log progress; null for nowhere.</param>
    public static void CreateBackupCertificate(ServerConnection cnn,
        string masterKeyPassword,
        string certificateName,
        string? certificateFile = null,
        string? privateKeyFile = null,
        string? privateKeyPassword = null,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        if (certificateFile != null && (privateKeyFile == null || privateKeyPassword == null))
            throw new ArgumentException("Exporting the certificate needs a file and a password for its private key.", nameof(privateKeyFile));

        Use(cnn, server =>
        {
            var masterDb = server.Databases["master"];
            if (masterDb.Certificates[certificateName] != null)
                throw new InvalidOperationException(
                    $"The certificate {certificateName} already exists in master. Backups encrypted with it can't be restored without it, so it isn't replaced.");

            if (masterDb.MasterKey == null)
            {
                logger.LogInformation("Creating the database master key in master");
                var masterKey = new MasterKey { Parent = masterDb };
                masterKey.Create(masterKeyPassword);
            }

            logger.LogInformation("Creating the certificate {Certificate} in master", certificateName);
            var certificate = new Certificate(masterDb, certificateName)
            {
                StartDate = DateTime.Today,
                Subject = "Backup certificate",
                ExpirationDate = new DateTime(2100, 12, 31)
            };
            certificate.Create();

            if (certificateFile != null)
            {
                // Without these files, backups encrypted with the certificate can't be restored on another server
                logger.LogInformation("Exporting the certificate to {CertificateFile} and its private key to {PrivateKeyFile}",
                    certificateFile, privateKeyFile);
                certificate.Export(certificateFile, privateKeyFile, privateKeyPassword);
            }
        });
    }

    #endregion

    #region Backup

    /// <summary>
    /// Backs up the database to a new file in the server's backup directory and returns its path.
    /// BACKUP DATABASE [AdventureWorks] TO DISK = N'/var/opt/mssql/data/dbHelper_AdventureWorks_20260101_120000_000.bak'
    /// WITH COPY_ONLY, DESCRIPTION = N'dbHelper BACKUP AdventureWorks', FORMAT, INIT, NAME = N'COPY ONLY(FULL) dbHelper BACKUP AdventureWorks',
    /// SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 5, CHECKSUM
    /// </summary>
    /// <param name="cnn">The connection.</param>
    /// <param name="encryption">How to encrypt the backup; null not to. The certificate must exist.</param>
    /// <param name="verify">Whether to check that the backup can be restored (RESTORE VERIFYONLY).</param>
    /// <param name="useCompression">Whether to compress the backup, on editions that can.</param>
    /// <param name="copyOnly">
    /// Whether the backup is outside the backup sequence. A copy-only backup doesn't start a log chain:
    /// after a switch to the full recovery model, only a backup that isn't copy-only does.
    /// </param>
    /// <param name="logger">Where to log progress; null for nowhere.</param>
    public static string BackupDatabase(ServerConnection cnn,
        BackupEncryptionOptions? encryption = null,
        bool verify = true,
        bool useCompression = true,
        bool copyOnly = true,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            var backupFileName = ServerPaths.Combine(server, server.BackupDirectory,
                $"{BackupFilePrefix}{dataBaseName}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.bak");
            logger.LogInformation("Backup file: {BackupFile}", backupFileName);

            var backup = new Backup
            {
                Action = BackupActionType.Database,
                Database = dataBaseName,
                CopyOnly = copyOnly,
                Checksum = true,
                Incremental = false,
                BackupSetDescription = BackupDescription(dataBaseName),
                BackupSetName = $"{(copyOnly ? "COPY ONLY(FULL)" : "FULL")} dbHelper BACKUP {dataBaseName}",
                ContinueAfterError = false,
                SkipTapeHeader = true,
                UnloadTapeAfter = false,
                NoRewind = true,
                FormatMedia = true,
                Initialize = true,
                PercentCompleteNotification = 5
            };

            // Depends on SQL Server Edition
            if (useCompression && server.EngineEdition is Edition.EnterpriseOrDeveloper or Edition.Standard)
                backup.CompressionOption = BackupCompressionOptions.On;

            if (encryption is { NoEncryption: false })
            {
                if (server.EngineEdition == Edition.Express)
                    throw new NotSupportedException($"Backup encryption isn't available on {server.Edition}.");
                // Never fall back to a backup that isn't encrypted when one that is was asked for
                if (encryption.EncryptorType == BackupEncryptorType.ServerCertificate &&
                    server.Databases["master"].Certificates[encryption.EncryptorName] == null)
                    throw new InvalidOperationException(
                        $"The certificate {encryption.EncryptorName} doesn't exist in master, so the backup can't be encrypted with it.");
                backup.EncryptionOption = encryption;
            }

            backup.PercentComplete += (_, e) => logger.LogInformation("Doing backup: {Percent}%", e.Percent);
            backup.Devices.AddDevice(backupFileName, DeviceType.File);
            backup.SqlBackup(server);

            if (verify)
            {
                logger.LogInformation("Checking the backup file");
                var restore = new Restore { Database = dataBaseName };
                restore.Devices.AddDevice(backupFileName, DeviceType.File);
                if (!restore.SqlVerify(server, out var verifyError))
                    throw new InvalidOperationException($"The backup file {backupFileName} can't be restored: {verifyError}");
                logger.LogInformation("The backup file can be restored");
            }

            return backupFileName;
        });
    }

    /// <summary>The newest dbHelper backup of the database, from the backup history in msdb; null if there is none.</summary>
    public static string? LastBackupFile(ServerConnection cnn)
    {
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, _ => Query(cnn,
            """
            SELECT TOP (1) mf.physical_device_name
            FROM msdb.dbo.backupset AS bs
                 INNER JOIN msdb.dbo.backupmediafamily AS mf ON mf.media_set_id = bs.media_set_id
            WHERE bs.database_name = @database AND bs.type = 'D' AND bs.description = @description
            ORDER BY bs.backup_finish_date DESC, bs.backup_set_id DESC;
            """,
            reader => reader.GetString(0),
            new SqlParameter("@database", dataBaseName),
            new SqlParameter("@description", BackupDescription(dataBaseName))).SingleOrDefault());
    }

    /// <summary>Is there a dbHelper backup of the database, and is its file still there?</summary>
    public static bool IsThereAnyBackupTaken(ServerConnection cnn)
    {
        var lastBackup = LastBackupFile(cnn);
        return lastBackup != null &&
               DetermineNumberOfBackupFiles(cnn).Paths.Contains(lastBackup, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Backup files

    /// <summary>
    /// The dbHelper backup files of the database in the server's backup directory, read with
    /// sys.dm_os_enumerate_filesystem (SQL Server 2017 and later, undocumented).
    /// </summary>
    public static BackupFiles DetermineNumberOfBackupFiles(ServerConnection cnn, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            var files = Query(cnn,
                """
                SELECT full_filesystem_path, size_in_bytes
                FROM sys.dm_os_enumerate_filesystem(@directory, @pattern)
                WHERE is_directory = 0
                ORDER BY last_write_time DESC, full_filesystem_path DESC;
                """,
                reader => (Path: reader.GetString(0), Size: reader.GetInt64(1)),
                new SqlParameter("@directory", server.BackupDirectory),
                new SqlParameter("@pattern", $"{BackupFilePrefix}{dataBaseName}_*.bak"));

            var result = new BackupFiles(files.Select(f => f.Path).ToList(), files.Sum(f => f.Size));
            logger.LogInformation("{Count} backup files of {Database} take {Size} bytes", result.Count, dataBaseName, result.TotalSizeInBytes);
            return result;
        });
    }

    /// <summary>
    /// Deletes the database's dbHelper backup files but the newest ones, with xp_delete_file
    /// (undocumented; it deletes only SQL Server backup files). Returns how many it deleted.
    /// </summary>
    /// <param name="cnn">The connection.</param>
    /// <param name="keepLatest">How many of the newest files to keep. The default keeps the one <see cref="RestoreDatabase"/> restores.</param>
    /// <param name="logger">Where to log progress; null for nowhere.</param>
    public static int DeleteBackupFiles(ServerConnection cnn, int keepLatest = 1, ILogger? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keepLatest);
        logger ??= NullLogger.Instance;
        return Use(cnn, _ =>
        {
            var toDelete = DetermineNumberOfBackupFiles(cnn).Paths.Skip(keepLatest).ToList();
            foreach (var file in toDelete)
            {
                logger.LogInformation("Deleting {BackupFile}", file);
                Execute(cnn, "EXECUTE master.dbo.xp_delete_file 0, @file;", new SqlParameter("@file", file));
            }

            // xp_delete_file doesn't say when it can't delete a file
            var left = DetermineNumberOfBackupFiles(cnn).Paths.Intersect(toDelete, StringComparer.OrdinalIgnoreCase).ToList();
            if (left.Count > 0)
                throw new InvalidOperationException($"These backup files couldn't be deleted: {string.Join(", ", left)}");

            return toDelete.Count;
        });
    }

    #endregion

    #region Free space

    /// <summary>Converts a size in MB, as SMO gives database sizes, to another unit.</summary>
    public static double DetermineSize(double sizeInMb, DiskSizeUnit diskSizeUnit) =>
        sizeInMb * Math.Pow(1024, (int)DiskSizeUnit.MegaBytes - (int)diskSizeUnit);

    /// <summary>
    /// The free space on the drive that holds the server's backup directory, from
    /// sys.dm_os_enumerate_fixed_drives (SQL Server 2017 and later, on Windows and Linux).
    /// </summary>
    public static double DetermineFreeSpaceForBackups(ServerConnection cnn, DiskSizeUnit unitOfMeasure = DiskSizeUnit.MegaBytes) =>
        Use(cnn, server =>
        {
            var drives = Query(cnn,
                "SELECT fixed_drive_path, free_space_in_bytes FROM sys.dm_os_enumerate_fixed_drives;",
                reader => (Path: reader.GetString(0), FreeBytes: reader.GetInt64(1)));

            // The longest drive path the backup directory starts with: the drive it is on
            var comparison = ServerPaths.IsLinux(server) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var backupDirectory = server.BackupDirectory;
            var drive = drives.Where(d => backupDirectory.StartsWith(d.Path, comparison))
                .OrderByDescending(d => d.Path.Length)
                .FirstOrDefault();
            if (drive.Path == null)
                throw new InvalidOperationException($"No fixed drive holds the backup directory {backupDirectory}.");

            return drive.FreeBytes / Math.Pow(1024, (int)unitOfMeasure);
        });

    /// <summary>
    /// Is there room for a backup of the database on the drive that holds the server's backup directory?
    /// The database's size, data and log, stands for the size of the backup.
    /// </summary>
    public static bool CanIPerformABackup(ServerConnection cnn, DiskSizeUnit unitOfMeasure = DiskSizeUnit.MegaBytes, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            logger.LogInformation("Checking free disk space");
            var dataBaseSize = DetermineSize(GetDatabase(server, dataBaseName).Size, unitOfMeasure);
            var freeToUse = DetermineFreeSpaceForBackups(cnn, unitOfMeasure);
            var enough = freeToUse > dataBaseSize;
            logger.LogInformation("Free for backups: {Free} {Unit}; the expected backup size: {Size:N8} {Unit}; enough: {Enough}",
                freeToUse, unitOfMeasure, dataBaseSize, unitOfMeasure, enough);
            return enough;
        });
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores the database over itself, from a backup file or else from its newest dbHelper backup,
    /// and returns the file it restored. Other users' sessions are killed first.
    /// RESTORE DATABASE [AdventureWorks] FROM DISK = N'...' WITH NOUNLOAD, REPLACE, STATS = 5
    /// </summary>
    public static string RestoreDatabase(ServerConnection cnn, string? backupFile = null, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            backupFile ??= LastBackupFile(cnn) ??
                           throw new InvalidOperationException($"There is no dbHelper backup of {dataBaseName} to restore.");
            logger.LogInformation("Restoring the database {Database} from {BackupFile}", dataBaseName, backupFile);

            // Leave the database, so that the restore can replace it
            cnn.SqlConnectionObject.ChangeDatabase("master");

            var db = server.Databases[dataBaseName];
            if (db != null)
            {
                server.KillAllProcesses(dataBaseName);
                db.UserAccess = DatabaseUserAccess.Single;
                db.Alter(TerminationClause.RollbackTransactionsImmediately);
            }

            var restore = new Restore
            {
                Action = RestoreActionType.Database,
                Database = dataBaseName,
                ReplaceDatabase = true,
                NoRecovery = false,
                PercentCompleteNotification = 5
            };
            restore.PercentComplete += (_, e) => logger.LogInformation("Restoring: {Percent}%", e.Percent);
            restore.Devices.AddDevice(backupFile, DeviceType.File);

            try
            {
                restore.SqlRestore(server);
            }
            finally
            {
                // A backup taken in single-user mode comes back in it, and so does the database if the restore failed
                server.Databases.Refresh();
                var restored = server.Databases[dataBaseName];
                if (restored is { UserAccess: not DatabaseUserAccess.Multiple })
                {
                    restored.UserAccess = DatabaseUserAccess.Multiple;
                    restored.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
            }

            return backupFile;
        });
    }

    #endregion

    #region CheckDB

    /// <summary>
    /// Checks the database's allocations, catalog and tables, and returns what the checks reported: nothing for a healthy database.
    /// DBCC CHECKALLOC, DBCC CHECKCATALOG and DBCC CHECKDB WITH NO_INFOMSGS, DATA_PURITY.
    /// </summary>
    /// <param name="cnn">The connection.</param>
    /// <param name="repairAllowDataLoss">
    /// Whether to repair what the checks find, losing data if it must (REPAIR_ALLOW_DATA_LOSS).
    /// It puts the database in single-user mode for the repair. Restoring a backup is the safer way out of corruption.
    /// </param>
    /// <param name="logger">Where to log progress; null for nowhere.</param>
    public static IReadOnlyList<string> CheckDb(ServerConnection cnn, bool repairAllowDataLoss = false, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            var repair = repairAllowDataLoss ? RepairType.AllowDataLoss : RepairType.None;
            if (repairAllowDataLoss)
                PutDbInUserMode(cnn, DatabaseUserAccess.Single, true, logger);

            try
            {
                var db = GetDatabase(server, dataBaseName);
                logger.LogInformation("Checking the database {Database}", dataBaseName);
                var messages = new List<string>();
                messages.AddRange(db.CheckAllocations(repair).Cast<string>());
                messages.AddRange(db.CheckCatalog().Cast<string>());
                messages.AddRange(db.CheckTables(repair, RepairOptions.NoInformationMessages, RepairStructure.DataPurity).Cast<string>());
                // Message 2528, which each DBCC command ends with, says nothing about the database
                return messages.Where(m => !m.StartsWith("DBCC execution completed.", StringComparison.Ordinal)).ToList();
            }
            finally
            {
                if (repairAllowDataLoss)
                    PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger);
            }
        });
    }

    #endregion

    #region User mode

    /// <summary>
    /// Puts the database in a user access mode.
    /// ALTER DATABASE [AdventureWorks] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    /// </summary>
    public static void PutDbInUserMode(ServerConnection cnn,
        DatabaseUserAccess mode,
        bool killUserProcess,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        Use(cnn, server =>
        {
            var db = GetDatabase(server, dataBaseName);
            if (db.UserAccess == mode)
            {
                logger.LogInformation("The database {Database} is already in mode {Mode}", dataBaseName, mode);
                return;
            }

            logger.LogInformation("Putting the database {Database} in mode {Mode}", dataBaseName, mode);
            db.UserAccess = mode;
            if (killUserProcess)
                server.KillAllProcesses(dataBaseName);
            db.Alter(TerminationClause.RollbackTransactionsImmediately);
            db.Refresh();
        });
    }

    /// <summary>Kills every other session that uses the database.</summary>
    public static void KillAllProcessesForOneDatabase(ServerConnection cnn)
    {
        var dataBaseName = cnn.DatabaseName;
        Use(cnn, server => server.KillAllProcesses(dataBaseName));
    }

    #endregion

    #region Recovery model and log

    /// <summary>
    /// Sets the database's recovery model.
    /// ALTER DATABASE [AdventureWorks] SET RECOVERY SIMPLE WITH ROLLBACK IMMEDIATE
    /// </summary>
    public static void SetRecoveryMode(
        ServerConnection cnn,
        RecoveryModel dbMode,
        bool killUserProcess,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        Use(cnn, server =>
        {
            var db = GetDatabase(server, dataBaseName);
            if (db.RecoveryModel == dbMode)
            {
                logger.LogInformation("The database {Database} is already in recovery model {Model}", dataBaseName, dbMode);
                return;
            }

            logger.LogInformation("Setting the recovery model of {Database} to {Model}", dataBaseName, dbMode);
            db.RecoveryModel = dbMode;
            if (killUserProcess)
            {
                server.KillAllProcesses(dataBaseName);
                db.Alter(TerminationClause.RollbackTransactionsImmediately);
            }
            else
            {
                db.Alter();
            }

            db.Refresh();
        });
    }

    /// <summary>The size of the database's log files together, in MB.</summary>
    public static double DetermineLogSize(ServerConnection cnn)
    {
        var dataBaseName = cnn.DatabaseName;
        // LogFile.Size is in KB
        return Use(cnn, server => GetDatabase(server, dataBaseName).LogFiles.Cast<LogFile>().Sum(f => f.Size) / 1024);
    }

    /// <summary>
    /// Shrinks the database's first log file towards a target size.
    /// DBCC SHRINKFILE(N'AdventureWorks2017_log', 256)
    /// </summary>
    public static LogShrinkResult ShrinkLog(ServerConnection cnn, int targetLogFileSizeMb, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            var db = GetDatabase(server, dataBaseName);
            var logFile = db.LogFiles[0];
            var before = logFile.Size / 1024;
            logger.LogInformation("Recovery model {Model}; the log is {Size} MB before shrinking", db.RecoveryModel, before);

            // TRUNCATEONLY would ignore the target size
            logFile.Shrink(targetLogFileSizeMb, ShrinkMethod.Default);
            logFile.Refresh();

            var result = new LogShrinkResult(before, logFile.Size / 1024);
            logger.LogInformation("The log is {Size} MB after shrinking, {Reduced} MB less", result.AfterMb, result.ReducedMb);
            return result;
        });
    }

    /// <summary>
    /// Shrinks the log of a database in the full recovery model: switches it to the simple recovery model,
    /// shrinks the log, switches it back and takes a full backup, which starts the log chain again.
    /// The log backups taken before can't be restored past the switch.
    /// </summary>
    public static LogShrinkResult ForceShrinkingLog(ServerConnection cnn, int targetLogSizeInMb, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        return Use(cnn, server =>
        {
            var wasFull = GetDatabase(server, dataBaseName).RecoveryModel == RecoveryModel.Full;
            if (wasFull)
                SetRecoveryMode(cnn, RecoveryModel.Simple, true, logger);

            LogShrinkResult result;
            try
            {
                result = ShrinkLog(cnn, targetLogSizeInMb, logger);
            }
            finally
            {
                if (wasFull)
                    SetRecoveryMode(cnn, RecoveryModel.Full, true, logger);
            }

            if (wasFull)
                BackupDatabase(cnn, copyOnly: false, logger: logger);
            return result;
        });
    }

    #endregion

    #region Is the database online?

    /// <summary>
    /// Is the database online? False if it is offline, suspect, restoring, missing or can't be opened.
    /// T-SQL equivalent: SELECT DATABASEPROPERTYEX(N'AdventureWorks', 'Status')
    /// </summary>
    public static bool IsTheDataBaseOnLine(ServerConnection cnn, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var dataBaseName = cnn.DatabaseName;
        bool online;
        try
        {
            online = Use(cnn, server => server.Databases[dataBaseName] is { } db && db.Status.HasFlag(DatabaseStatus.Normal));
        }
        catch (Exception ex) when (CannotOpenDatabase(ex))
        {
            // The connection opens the database, which fails when it is offline or missing
            online = false;
        }

        logger.LogInformation("The database {Database} is {State}", dataBaseName, online ? "ONLINE" : "NOT ONLINE");
        return online;
    }

    #endregion

    #region Helpers

    private static T Use<T>(ServerConnection cnn, Func<Server, T> action)
    {
        var connectHere = !cnn.IsOpen;
        if (connectHere)
            cnn.Connect();
        try
        {
            return action(new Server(cnn));
        }
        finally
        {
            if (connectHere && cnn.IsOpen)
                cnn.Disconnect();
        }
    }

    private static void Use(ServerConnection cnn, Action<Server> action) =>
        Use(cnn, server =>
        {
            action(server);
            return true;
        });

    private static Database GetDatabase(Server server, string dataBaseName) =>
        server.Databases[dataBaseName] ??
        throw new InvalidOperationException($"The database {dataBaseName} doesn't exist on {server.Name}.");

    private static string BackupDescription(string dataBaseName) => $"dbHelper BACKUP {dataBaseName}";

    private static List<T> Query<T>(ServerConnection cnn, string sql, Func<SqlDataReader, T> read, params SqlParameter[] parameters)
    {
        using var command = new SqlCommand(sql, cnn.SqlConnectionObject);
        command.Parameters.AddRange(parameters);
        using var reader = command.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read())
            rows.Add(read(reader));
        return rows;
    }

    private static void Execute(ServerConnection cnn, string sql, params SqlParameter[] parameters)
    {
        using var command = new SqlCommand(sql, cnn.SqlConnectionObject);
        command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }

    // 4060: cannot open the database; 942: it is offline; 976: it is a secondary replica that can't be read
    private static bool CannotOpenDatabase(Exception ex) =>
        ex.CollectThemAll(e => e.InnerException)
            .OfType<SqlException>()
            .Any(e => e.Errors.Cast<SqlError>().Any(error => error.Number is 4060 or 942 or 976));

    #endregion
}
