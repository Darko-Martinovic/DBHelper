using DBHelper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

// based on Microsoft https://learn.microsoft.com/en-us/sql/relational-databases/security/encryption/move-a-tde-protected-database-to-another-sql-server
public sealed class Tde : IExample
{
    public const string DbName = "CustRecords";
    public const string CertName = "TdeCertificate";

    public string Description => "Shows how to encrypt the database";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");
            if (server.EngineEdition == Edition.Express)
            {
                Console.WriteLine($"Not available on {server.Edition}");
                return;
            }

            // Drop the database if exists
            server.Databases[DbName]?.Drop();

            //Create the database
            var db = new Database(server, DbName);
            db.Create();

            // get the reference to the master database
            var masterDb = server.Databases["master"];

            // The master key and the certificate protect every database encrypted with them:
            // create them when they are missing, never drop them
            if (masterDb.MasterKey == null)
            {
                Console.WriteLine("Create the master key");
                var mk = new MasterKey { Parent = masterDb };
                mk.Create(context.RequireKeyPassword());
            }

            if (masterDb.Certificates[CertName] == null)
            {
                //Creating certificate
                Console.WriteLine($"Create the certificate {CertName}");
                var certificate = new Certificate(masterDb, CertName)
                {
                    StartDate = DateTime.Today,
                    Subject = "Certificate to protect TDE key"
                };
                certificate.Create();

                // Without a backup of the certificate, the database can't be restored on another server
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var certificateFile = ServerPaths.Combine(server, server.BackupDirectory, $"{CertName}_{stamp}.cer");
                var privateKeyFile = ServerPaths.Combine(server, server.BackupDirectory, $"{CertName}_{stamp}.pvk");
                certificate.Export(certificateFile, privateKeyFile, context.RequireKeyPassword());
                ConsoleEx.WriteLine($"The certificate is backed up to {certificateFile} and its private key to {privateKeyFile}, on the server",
                    ConsoleColor.Yellow);
            }

            //Create Database Encryption
            var dbEk = new DatabaseEncryptionKey
            {
                Parent = db,
                EncryptionAlgorithm = DatabaseEncryptionAlgorithm.Aes256,
                EncryptionType = DatabaseEncryptionType.ServerCertificate,
                EncryptorName = CertName
            };

            // Just show in console the database encryption key status
            var so = new ScriptingOptions
            {
                IncludeHeaders = true,
                IncludeIfNotExists = true
            };

            Console.WriteLine(new string('-', 79));
            Console.WriteLine("T-SQL for create");
            ConsoleEx.WriteScript(dbEk.Script(so).Cast<string>());

            dbEk.Create();

            // Show another feature. How to capture t-sql
            server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;

            // Change the database compatibility mode
            db.CompatibilityLevel = CompatibilityLevel.Version140;
            db.Alter();

            // Reproduce T-SQL in Console
            Console.WriteLine(new string('-', 79));
            Console.WriteLine("T-SQL for Alter");
            ConsoleEx.WriteScript(server.ConnectionContext.CapturedSql.Text.Cast<string>());

            // Clearing the already stored TSQL
            server.ConnectionContext.CapturedSql.Clear();

            // Changing the execution mode back to normal
            server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;

            db.EncryptionEnabled = true;
            db.Alter();
            ConsoleEx.WriteLine($"The database {DbName} is encrypted", ConsoleColor.Cyan);
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
