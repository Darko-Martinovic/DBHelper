using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Converter.Extension;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction
{
    class RebuildIndexes
    {
        private static Server _server;

        static void Main()
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;

            const int fillFactor = 100;

            ServerConnection cnn;
            string databaseName;

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
                // Read the database name from app.config
                databaseName = sqlConnection.Database;
            }

            cnn.Connect();

            Console.WriteLine("Connected");
            _server = new Server(cnn);
            Console.WriteLine("Create the server object");

            Console.WriteLine($@"Rebuilding indexes for database {databaseName}");

            var db = _server.Databases[databaseName];

            foreach (Table tbl in db.Tables)
            {
                if (tbl.IsSystemObject)
                    continue;
                if (tbl.IsMemoryOptimized)
                    continue;
               
                try
                {
                    Console.WriteLine($@"Rebuilding indexes for table : {tbl.Schema}.{tbl.Name}");
                    //
                    // DBCC DBREINDEX(N'{0}', N'', {1}) is executed
                    //

                    tbl.RebuildIndexes(fillFactor);
                    Console.WriteLine($@"Updating statistics for table : {tbl.Schema}.{tbl.Name}");
                    //
                    // "UPDATE STATISTICS <table name> <statistics name> is executed"
                    //
                    tbl.UpdateStatistics(StatisticsTarget.Column, StatisticsScanType.FullScan);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                        .Select(ex1 => ex1.Message)));
                    Console.ReadLine();
                }

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