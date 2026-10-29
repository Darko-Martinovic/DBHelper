using DBHelper;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

/// <summary>
/// Runs DbGeneral's maintenance tasks one after another, as the Tester console did, on a database of
/// its own: the restores replace it.
/// </summary>
public sealed class DbHelperTour : IExample
{
    public const string CertName = "DbHelperBackupCertificate";

    public string Description => "Runs the DBHelper library's tasks: backup, restore, encryption, backup files, free space, log, CHECKDB";

    public void Run(ExampleContext context)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(o => o.SingleLine = true));
        var logger = loggerFactory.CreateLogger("DbGeneral");
        var databaseName = context.Settings.TourDatabase;
        CreateDatabaseIfMissing(context, databaseName);
        var cnn = context.Connect(databaseName);

        Step("Is the database ONLINE", () => $"online: {DbGeneral.IsTheDataBaseOnLine(cnn, logger)}");
        Step("Put the database in restricted user access mode",
            () => DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Restricted, true, logger));
        Step("Put the database in multiple user access mode",
            () => DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger));
        Step("Backup the database", () => DbGeneral.BackupDatabase(cnn, logger: logger));
        Step("Restore the database", () => DbGeneral.RestoreDatabase(cnn, logger: logger));

        var encryption = new BackupEncryptionOptions
        {
            NoEncryption = false,
            Algorithm = BackupEncryptionAlgorithm.Aes256,
            EncryptorType = BackupEncryptorType.ServerCertificate,
            EncryptorName = CertName
        };
        Step("Backup the database WITH encryption", () =>
        {
            if (!DbGeneral.BackupCertificateExists(cnn, CertName))
            {
                // Export the certificate: without it, the backup can't be restored on another server
                var directory = BackupDirectory(cnn);
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                DbGeneral.CreateBackupCertificate(cnn, context.RequireKeyPassword(), CertName,
                    directory($"{CertName}_{stamp}.cer"), directory($"{CertName}_{stamp}.pvk"), context.RequireKeyPassword(), logger);
            }

            return DbGeneral.BackupDatabase(cnn, encryption, logger: logger);
        });
        Step("Restore the database", () => DbGeneral.RestoreDatabase(cnn, logger: logger));
        Step("Is there a backup", () => $"backup taken: {DbGeneral.IsThereAnyBackupTaken(cnn)}");
        Step("How many backup files are there", () =>
        {
            var files = DbGeneral.DetermineNumberOfBackupFiles(cnn, logger);
            return $"{files.Count} files, {files.TotalSizeInBytes:N0} bytes";
        });
        Step("Delete old backup files, but the newest", () => $"deleted {DbGeneral.DeleteBackupFiles(cnn, logger: logger)}");
        Step("Determine free disk space",
            () => $"room for a backup: {DbGeneral.CanIPerformABackup(cnn, DiskSizeUnit.GigaBytes, logger)}");
        Step("Force shrinking the log", () =>
        {
            var result = DbGeneral.ForceShrinkingLog(cnn, 8, logger);
            return $"{result.BeforeMb} MB to {result.AfterMb} MB";
        });
        Step("CHECKDB", () =>
        {
            var messages = DbGeneral.CheckDb(cnn, logger: logger);
            return messages.Count == 0 ? "no errors" : string.Join(Environment.NewLine, messages);
        });
    }

    private static void Step(string task, System.Action action) =>
        Step(task, () =>
        {
            action();
            return "";
        });

    // A task that fails doesn't stop the tour
    private static void Step(string task, Func<object> action)
    {
        ConsoleEx.WriteLine(new string('-', 79), ConsoleColor.Cyan);
        ConsoleEx.WriteLine(task, ConsoleColor.Cyan);
        try
        {
            var result = action();
            ConsoleEx.WriteLine($"The task finished successfully! {result}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            ConsoleEx.WriteLine($"The task failed with following error message: {ex.AllMessages()}", ConsoleColor.Red);
        }
    }

    private static void CreateDatabaseIfMissing(ExampleContext context, string databaseName)
    {
        var cnn = context.Connect("master");
        try
        {
            var server = new Server(cnn);
            if (server.Databases[databaseName] != null)
                return;

            ConsoleEx.WriteLine($"Creating the database {databaseName}", ConsoleColor.Yellow);
            var db = new Database(server, databaseName) { RecoveryModel = RecoveryModel.Full };
            db.Create();
            db.ExecuteNonQuery("CREATE TABLE dbo.Visit (Id int IDENTITY PRIMARY KEY, VisitedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()); INSERT dbo.Visit DEFAULT VALUES;");
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }

    private static Func<string, string> BackupDirectory(Microsoft.SqlServer.Management.Common.ServerConnection cnn)
    {
        var server = new Server(cnn);
        var directory = server.BackupDirectory;
        return fileName => ServerPaths.Combine(server, directory, fileName);
    }
}
