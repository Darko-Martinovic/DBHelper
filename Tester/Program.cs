using DBHelper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;


namespace Tester
{


public class Program
    {

        private const string CNewline = "\r\n";

        static void Main(string[] args)
        {
            
            string errorMessage = string.Empty;


            String connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            var sqlConnection = new SqlConnection(connectionString);
            var cnn = new ServerConnection(sqlConnection);
            ILog logger = new Logger();

            //-------------------------------------------------------------
            // Is the database ONLINE
            //-------------------------------------------------------------
            Console.WriteLine(DbGeneral.IsTheDataBaseOnLine(cnn, logger, ref errorMessage)
                ? "The task of determining the database state finished successfully!"
                : $"The task of determining the database state failed with following error message :{errorMessage}");


            Console.WriteLine("..............................................................................................");

            //-------------------------------------------------------------
            // Put the database in Restricted UserAccess Mode
            //-------------------------------------------------------------
            Console.WriteLine(
                DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Restricted, true, logger, ref errorMessage)
                    ? "The task of putting the database in restricted user access mode finished successfully!"
                    : $"The task of putting the database in restricted user access mode failed with following error message :{errorMessage}");

            Console.WriteLine("..............................................................................................");

            //-------------------------------------------------------------
            // Put the database in Multiple UserAccess Mode
            //-------------------------------------------------------------
            Console.WriteLine(
                DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger, ref errorMessage)
                    ? "The task of putting the database in multiple user access mode finished successfully!"
                    : $"The task of putting the database in multiple user access mode failed with following error message :{errorMessage}");

            Console.WriteLine("..............................................................................................");

            //-------------------------------------------------------------
            // Test Backup
            //-------------------------------------------------------------

            Console.WriteLine(DbGeneral.BackupDatabase(cnn, logger, ref errorMessage, true)
                ? "The task of backup the database finsihed successfully!"
                : $"The task of backup the database failed with following error message : {errorMessage}");

            Console.WriteLine("..............................................................................................");



            //-------------------------------------------------------------
            // Test restore
            //-------------------------------------------------------------
            Console.WriteLine(DbGeneral.RestoreDatabase(cnn, logger, ref errorMessage)
                ? "The task of restore the database finsihed successfully!"
                : $"The task of restore the database failed with following error message : {errorMessage}");


            Console.WriteLine("..............................................................................................");



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


            Console.WriteLine("..............................................................................................");


            //Frist be sure that you published SQLCLR project, then uncomment following lines


            ////-------------------------------------------------------------
            //// How many backup are taken
            ////-------------------------------------------------------------
            var numberOfBackupFiles = 0;
            long totalSizeOfBackupFiles = 0;
            Console.WriteLine(
                DbGeneral.DetermineNumberOfBackupFiles(cnn, logger, ref numberOfBackupFiles, ref totalSizeOfBackupFiles,
                    ref errorMessage)
                    ? "The task of determining how many backup files are taken finished successfully!"
                    : $"The task of determining how many backup files are taken  failed with following error message :{errorMessage}");


            Console.WriteLine("..............................................................................................");




            ////-------------------------------------------------------------
            //// Delete old backup files
            ////-------------------------------------------------------------
            Console.WriteLine(DbGeneral.DeleteBackupFiles(cnn, logger, ref errorMessage)
                ? "The task of deleting old backup files finished successfully!"
                : $"The task of deleting old backup files failed with following error message :{errorMessage}");

            Console.WriteLine("..............................................................................................");



            ////-------------------------------------------------------------
            //// Determine free disk space
            ////-------------------------------------------------------------
            Console.WriteLine(DbGeneral.CanIPerformABackup(cnn, logger, ref errorMessage)
                ? "The task of determining free disk space finished successfully!"
                : $"The task of determining free disk space failed with following error message :{errorMessage}");


            Console.WriteLine("..............................................................................................");




            //-------------------------------------------------------------
            // Force shrinking log
            //-------------------------------------------------------------


            Console.WriteLine(DbGeneral.ForceShrinkingLog(cnn, 256, true, logger, ref errorMessage)
                ? "The task of shrinking log finished successfully!"
                : $"The task of shrinking log failed with following error message :{errorMessage}");


            Console.WriteLine("..............................................................................................");



            //-------------------------------------------------------------
            // Test CheckDb
            //-------------------------------------------------------------
            Console.WriteLine(DbGeneral.CheckDb(cnn, logger, ref errorMessage)
                ? "The task of checking the database finsihed successfully!"
                : $"The task of checking the database failed with following error message: {errorMessage}");

            Console.WriteLine("..............................................................................................");



            Console.Write($"Press any key to exit...{CNewline}");
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
