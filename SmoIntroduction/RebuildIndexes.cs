using DBHelper;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

public sealed class RebuildIndexes : IExample
{
    // fill factor
    private const int FillFactor = 100;

    public string Description => "Shows how to rebuild indexes and update statistics";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();
        var databaseName = context.DatabaseName;

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");

            Console.WriteLine($@"Rebuilding indexes for database {databaseName}");

            var db = server.Databases[databaseName];

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
                    tbl.RebuildIndexes(FillFactor);
                    Console.WriteLine($@"Updating statistics for table : {tbl.Schema}.{tbl.Name}");
                    //
                    // "UPDATE STATISTICS <table name> <statistics name> is executed"
                    //
                    tbl.UpdateStatistics(StatisticsTarget.Column, StatisticsScanType.FullScan);
                }
                catch (Exception ex)
                {
                    // One table that can't be rebuilt doesn't stop the others
                    ConsoleEx.WriteLine(ex.AllMessages(), ConsoleColor.Red);
                }
            }
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
