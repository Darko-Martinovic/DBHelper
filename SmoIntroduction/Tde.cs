using System;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Converter.Extension;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using static Microsoft.SqlServer.Management.Smo.Edition;

namespace SmoIntroduction
{

    // based on Microsoft https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/move-a-tde-protected-database-to-another-sql-server?view=sql-server-2017

    class Tde
    {
        private static Server _server;

        private static string _dbName = "CustRecords";

        private static string _certName = @"TestSQLServerCert";
        private static string _password = @"*rt@40(FL&dasl1";

        private static string _privateKeyFile = @"C:\TMP\SQLPrivateKeyFile";
        private static string _certfile = @"C:\TMP\TestSQLServerCert";

        static void Main()
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            ServerConnection cnn;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
            }

            cnn.Connect();

            Console.WriteLine("Connected");
            _server = new Server(cnn);
            Console.WriteLine("Create the server object");
            if (_server.EngineEdition == Express)
            {
                Console.WriteLine($"Not available on {_server.Edition}");
                Console.ReadLine();
                return;

            }

            // Drop the database if exists
            if (_server.Databases[_dbName] != null)
                _server.Databases[_dbName].Drop();
            
            //Create the database
            var db = new Database(_server, _dbName);
            db.Create();

            // get the reference to the master database
            var masterDb = _server.Databases["master"];

            // Drop certificate and master key
            masterDb.Certificates[_certName]?.Drop();
            //
            masterDb.MasterKey?.Drop();

            //Create the master key
            var mk = new MasterKey {Parent = masterDb};
            mk.Create(_password);
            mk.Open(_password);

            //Creating certificate 
            var certificate = new Certificate(masterDb, _certName)
            {
                StartDate = DateTime.Today,
                Subject = "Certificate to protect TDE key"
            };
            certificate.Create();

            if (File.Exists(_certfile))
                File.Delete(_certfile);
            if (File.Exists(_privateKeyFile))
                File.Delete(_privateKeyFile);

            //Create a backup of the server certificate in the master database.  
            certificate.Export(_certfile, _privateKeyFile, _password);

            try
            {
                //Create Database Encryption 
                var dbEk = new DatabaseEncryptionKey
                {
                    Parent = db,
                    EncryptionAlgorithm = DatabaseEncryptionAlgorithm.Aes256,
                    EncryptionType = DatabaseEncryptionType.ServerCertificate,
                    EncryptorName = _certName
                };

                // Just show in console the database encryption key status
                var so = new ScriptingOptions
                {
                    IncludeHeaders = true,
                    IncludeIfNotExists = true
                };
                var sc = dbEk.Script(so);


                Console.WriteLine(new string('-', 79));
                Console.WriteLine("T-SQL for create");
                Console.WriteLine(new string('-', 79));
                foreach (var s in sc)
                {
                    Console.WriteLine(s);
                }

                dbEk.Create();


                // Show another feature. How to capture t-sql 
                _server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;

                // Change the database compatibility mode
                db.CompatibilityLevel = CompatibilityLevel.Version120;
                db.Alter();


                // Reproducte T-SQL in Console
                Console.WriteLine(new string('-', 79));
                Console.WriteLine("T-SQL for Alter");
                Console.WriteLine(new string('-', 79));
                foreach (var s in _server.ConnectionContext.CapturedSql.Text)
                {
                    Console.WriteLine(s);
                }

                // Clearing the already stored TSQL
                _server.ConnectionContext.CapturedSql.Clear();

                // Changing the execution mode back to normal
                _server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;

                db.EncryptionEnabled = true;
                db.Alter();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message)));
                Console.ReadLine();
            }
            finally
            {
                //db.Drop();
                //masterDb.Certificates[_certName].Drop();

            }
            if (cnn.IsOpen)
                cnn.Disconnect();
            cnn = null;
            db = null;
            _server = null;
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();



        }
    }

}
