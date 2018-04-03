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

        private const string C_NEWLINE = "\r\n";

        static void Main(string[] args)
        {
            
            string errorMessage = string.Empty;


            String connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            ServerConnection cnn = new ServerConnection(sqlConnection);
            ILog logger = new Logger();

            //-------------------------------------------------------------
            // Is the database is ONLINE
            //-------------------------------------------------------------
            if (DBGeneral.IsTheDataBaseOnLine(cnn, logger, ref errorMessage))
                Console.WriteLine("The task of determining the database state finished successfully!");
            else
                Console.WriteLine("The task of determining the database state failed with following error message :" + errorMessage);


            Console.WriteLine("..............................................................................................");

            //-------------------------------------------------------------
            // Put the database in Restricted UserAccess Mode
            //-------------------------------------------------------------
            if (DBGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Restricted, true, logger,ref errorMessage))
                Console.WriteLine("The task of putting the database in restricted user access mode finished successfully!");
            else
                Console.WriteLine("The task of putting the database in restricted user access mode failed with following error message :" + errorMessage);

            Console.WriteLine("..............................................................................................");

            //-------------------------------------------------------------
            // Put the database in Multiple UserAccess Mode
            //-------------------------------------------------------------
            if (DBGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger, ref errorMessage))
                Console.WriteLine("The task of putting the database in multiple user access mode finished successfully!");
            else
                Console.WriteLine("The task of putting the database in multiple user access mode failed with following error message :" + errorMessage);

            Console.WriteLine("..............................................................................................");

            //-------------------------------------------------------------
            // Test Backup
            //-------------------------------------------------------------
            
            if (DBGeneral.BackupDatabase(cnn, logger, ref errorMessage, true))
                Console.WriteLine("The task of backup the database finsihed successfully!");
            else
                Console.WriteLine("The task of backup the database failed with following error message : " + errorMessage);

            Console.WriteLine("..............................................................................................");



            //-------------------------------------------------------------
            // Test restore
            //-------------------------------------------------------------
            if (DBGeneral.RestoreDatabase(cnn, logger, ref errorMessage))
                Console.WriteLine("The task of restore the database finsihed successfully!");
            else
                Console.WriteLine("The task of restore the database failed with following error message : " + errorMessage);


            Console.WriteLine("..............................................................................................");



            //-------------------------------------------------------------
            // Is there last backup
            //-------------------------------------------------------------
            if (DBGeneral.IsThereAnyBackupTaken(cnn, logger, ref errorMessage))
                Console.WriteLine("The backup was taken!");
            else if (errorMessage != string.Empty)
                Console.WriteLine("There is an error when trying to determine is backup taken. The error message :" + errorMessage);
            else
                Console.WriteLine("There is no backup file!");


            Console.WriteLine("..............................................................................................");


            ///Frist be sure that you published SQLCLR project, then uncomment following lines


            ////-------------------------------------------------------------
            //// How many backup are taken
            ////-------------------------------------------------------------
            //int numberOfBackupFiles = 0;
            //Int64 totalSizeOfBackupFiles = 0;
            //if (DBGeneral.DetermineNumberOfBackupFiles(cnn, logger,ref numberOfBackupFiles,ref totalSizeOfBackupFiles, ref errorMessage))
            //    Console.WriteLine("The task of determining how many backup files are taken finished successfully!");
            //else
            //    Console.WriteLine("The task of determining how many backup files are taken  failed with following error message :" + errorMessage);


           Console.WriteLine("..............................................................................................");




            ////-------------------------------------------------------------
            //// Delete old backup files
            ////-------------------------------------------------------------
            if (DBGeneral.DeleteBackupFiles(cnn, logger, ref errorMessage))
                Console.WriteLine("The task of deleting old backup files finished successfully!");
            else
                Console.WriteLine("The task of deleting old backup files failed with following error message :" + errorMessage);

            Console.WriteLine("..............................................................................................");



            ////-------------------------------------------------------------
            //// Determine free disk space
            ////-------------------------------------------------------------
            if (DBGeneral.CanIPerformABackup(cnn, logger, ref errorMessage))
                Console.WriteLine("The task of determining free disk space finished successfully!");
            else
                Console.WriteLine("The task of determining free disk space failed with following error message :" + errorMessage);


            Console.WriteLine("..............................................................................................");




            //-------------------------------------------------------------
            // Force shrinking log
            //-------------------------------------------------------------


            if (DBGeneral.ForceShrinkingLog(cnn,256,true,logger,ref errorMessage))
                Console.WriteLine("The task of shrinking log finished successfully!");
            else
                Console.WriteLine("The task of shrinking log failed with following error message :" + errorMessage);


            Console.WriteLine("..............................................................................................");



            //-------------------------------------------------------------
            // Test CheckDb
            //-------------------------------------------------------------
            if (DBGeneral.CheckDb(cnn, logger, ref errorMessage))
                Console.WriteLine("The task of checking the database finsihed successfully!");
            else
                Console.WriteLine("The task of checking the database failed with following error message: " + errorMessage);

            Console.WriteLine("..............................................................................................");



            Console.Write("Press any key to exit..." + C_NEWLINE);
            Console.ReadLine();

        }
        public class Logger : ILog
        {
            public void Log(string text, bool newLine)
            {
                Console.WriteLine("\t" + text + (newLine ? "\r\n" : ""));
            }
        }
    }


}
