using DBHelper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;


namespace Tester
{


internal class Program
    {
        public static string _certName = @"TestSQLServerCert";
        public static string _password = @"*rt@40(FL&dasl1";

        static void Main(string[] args)
        {
            
            var errorMessage = string.Empty;


            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            var sqlConnection = new SqlConnection(connectionString);
            var cnn = new ServerConnection(sqlConnection);
            ILog logger = new Logger();






            //-------------------------------------------------------------
            // Is the database ONLINE
            //-------------------------------------------------------------
            ConsoleEx.WriteLine(DbGeneral.IsTheDataBaseOnLine(cnn, logger, ref errorMessage)
                ? "The task of determining the database state finished successfully!"
                : $"The task of determining the database state failed with following error message :{errorMessage}", ConsoleColor.Cyan);


            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);

            //-------------------------------------------------------------
            // Put the database in Restricted UserAccess Mode
            //-------------------------------------------------------------
            ConsoleEx.WriteLine(
                DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Restricted, true, logger, ref errorMessage)
                    ? "The task of putting the database in restricted user access mode finished successfully!"
                    : $"The task of putting the database in restricted user access mode failed with following error message :{errorMessage}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine("..............................................................................................", ConsoleColor.Yellow);

            //-------------------------------------------------------------
            // Put the database in Multiple UserAccess Mode
            //-------------------------------------------------------------
            ConsoleEx.WriteLine(
                DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger, ref errorMessage)
                    ? "The task of putting the database in multiple user access mode finished successfully!"
                    : $"The task of putting the database in multiple user access mode failed with following error message :{errorMessage}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);

            //-------------------------------------------------------------
            // Test Backup
            //-------------------------------------------------------------

            ConsoleEx.WriteLine(DbGeneral.BackupDatabase(cnn, logger, ref errorMessage)
                ? "The task of backup the database finsihed successfully!"
                : $"The task of backup the database failed with following error message : {errorMessage}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine(" ".PadRight(80, '-'), ConsoleColor.Cyan);



            //-------------------------------------------------------------
            // Test restore
            //-------------------------------------------------------------
            ConsoleEx.WriteLine(DbGeneral.RestoreDatabase(cnn, logger, ref errorMessage)
                ? "The task of restore the database finsihed successfully!"
                : $"The task of restore the database failed with following error message : {errorMessage}", ConsoleColor.Cyan);


            ConsoleEx.WriteLine(" ".PadRight(80, '-'), ConsoleColor.Cyan);



            //-------------------------------------------------------------
            // Test Backup WITH Encryption
            //-------------------------------------------------------------

            var beo = new BackupEncryptionOptions
            {
                NoEncryption = false,
                Algorithm = BackupEncryptionAlgorithm.Aes256,
                EncryptorType = BackupEncryptorType.ServerCertificate,
                EncryptorName = _certName
            };
            if (DbGeneral.EnsureBackupCertificateExists(cnn, logger, ref errorMessage,
                    _certName) == false)
            {
                if (DbGeneral.CreateBackupCertificate(cnn, logger, _password, _certName, ref errorMessage) == false)
                {
                    Console.WriteLine($"Error creating the server certificate : {errorMessage}");
                }
            }
            ConsoleEx.WriteLine(DbGeneral.BackupDatabase(cnn, logger, ref errorMessage, bckEncOpt: beo)
                ? "The task of backup the database with encryption finsihed successfully!"
                : $"The task of backup the database with encryption failed with following error message : {errorMessage}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine(" ".PadRight(80, '-'), ConsoleColor.Cyan);


            //-------------------------------------------------------------
            // Test restore
            //-------------------------------------------------------------
            ConsoleEx.WriteLine(DbGeneral.RestoreDatabase(cnn, logger, ref errorMessage)
                ? "The task of restore the database finsihed successfully!"
                : $"The task of restore the database failed with following error message : {errorMessage}", ConsoleColor.Cyan);


            ConsoleEx.WriteLine(" ".PadRight(80, '-'), ConsoleColor.Cyan);





            //-------------------------------------------------------------
            // Is there last backup
            //-------------------------------------------------------------
            if (DbGeneral.IsThereAnyBackupTaken(cnn, logger, ref errorMessage))
                Console.WriteLine("The backup was taken!");
            else if (errorMessage != string.Empty)
                Console.WriteLine(
                    $"There is an error when trying to determine is backup taken. The error message :{errorMessage}");
            else
                Console.WriteLine("There is no backup file!");


            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);


            //Frist be sure that you published SQLCLR project, then uncomment following lines


            ////-------------------------------------------------------------
            //// How many backup are taken
            ////-------------------------------------------------------------
            var numberOfBackupFiles = 0;
            long totalSizeOfBackupFiles = 0;
            ConsoleEx.WriteLine(
                DbGeneral.DetermineNumberOfBackupFiles(cnn, logger, ref numberOfBackupFiles, ref totalSizeOfBackupFiles,
                    ref errorMessage)
                    ? "The task of determining how many backup files are taken finished successfully!"
                    : $"The task of determining how many backup files are taken  failed with following error message :{errorMessage}", ConsoleColor.Cyan);


            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);




            ////-------------------------------------------------------------
            //// Delete old backup files
            ////-------------------------------------------------------------
            ConsoleEx.WriteLine(DbGeneral.DeleteBackupFiles(cnn, logger, ref errorMessage)
                ? "The task of deleting old backup files finished successfully!"
                : $"The task of deleting old backup files failed with following error message :{errorMessage}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);



            ////-------------------------------------------------------------
            //// Determine free disk space
            ////-------------------------------------------------------------
            ConsoleEx.WriteLine(DbGeneral.CanIPerformABackup(cnn, logger, ref errorMessage)
                ? "The task of determining free disk space finished successfully!"
                : $"The task of determining free disk space failed with following error message :{errorMessage}", ConsoleColor.Cyan);


            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);




            //-------------------------------------------------------------
            // Force shrinking log
            //-------------------------------------------------------------


            ConsoleEx.WriteLine(DbGeneral.ForceShrinkingLog(cnn, 256, logger, ref errorMessage)
                ? "The task of shrinking log finished successfully!"
                : $"The task of shrinking log failed with following error message :{errorMessage}", ConsoleColor.Cyan);


            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);



            //-------------------------------------------------------------
            // Test CheckDb
            //-------------------------------------------------------------
            ConsoleEx.WriteLine(DbGeneral.CheckDb(cnn, logger, ref errorMessage)
                ? "The task of checking the database finsihed successfully!"
                : $"The task of checking the database failed with following error message: {errorMessage}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine(" ".PadRight(80,'-'), ConsoleColor.Cyan);



            Console.WriteLine($"Press any key to exit...");
            Console.ReadLine();

        }
        public class Logger : ILog
        {
            public void Log(string text, bool newLine)
            {
                Console.WriteLine($"\t{text}{(newLine ? "\r\n" : "")}");
            }
        }
    }


}
