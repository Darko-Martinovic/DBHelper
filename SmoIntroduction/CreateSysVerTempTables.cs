using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

public sealed class CreateSysVerTempTables : IExample
{
    private const string CServerVersion = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545

    public string Description => "Shows how to create system-versioned ( temporal ) tables";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();
        var databaseName = context.DatabaseName;

        cnn.Connect();
        try
        {
            var server = new Server(cnn);

            if (server.Version < new Version(CServerVersion))
            {
                ConsoleEx.WriteLine("Only supported for SQL 2016+");
                return;
            }

            var db = server.Databases[databaseName];
            //Person.EmailAddress
            var tbl = db.Tables["EmailAddress", "Person"];

            if (tbl.IsSystemVersioned)
            {
                ConsoleEx.WriteLine($"{tbl.Schema}.{tbl.Name} is already system-versioned", ConsoleColor.Yellow);
            }
            else
            {
                // --== Let's define the first column 'ValidFrom' ==---
                //      SMO adds the period before the columns, so the columns are added first as plain
                //      DATETIME2 NOT NULL columns; adding the period then makes them GENERATED ALWAYS AS ROW START and END
                var col = new Column(tbl, "ValidFrom")
                {
                    DataType = DataType.DateTime2(7),
                    Nullable = false
                };
                // The rows already there need a value: they are valid since then
                col.AddDefaultConstraint("DfValidFrom");
                col.DefaultConstraint.Text = "'1991-01-01 00:00:00.0000000'";
                tbl.Columns.Add(col);

                // --== Let's define the column 'ValidTo' ==---
                var col2 = new Column(tbl, "ValidTo")
                {
                    DataType = DataType.DateTime2(7),
                    Nullable = false
                };
                col2.AddDefaultConstraint("DfValidTo");
                col2.DefaultConstraint.Text = "'9999-12-31 23:59:59.9999999'";
                tbl.Columns.Add(col2);
                tbl.Alter();

                // --== Let's define the period ==---
                tbl.AddPeriodForSystemTime(col.Name, col2.Name, true);
                tbl.Alter();

                tbl.IsSystemVersioned = true;
                tbl.HistoryTableSchema = tbl.Schema;
                tbl.HistoryTableName = $"{tbl.Name}_History";
                tbl.DataConsistencyCheck = true;
                tbl.Alter();
            }

            ConsoleEx.WriteLine("Let's Examine the Table ", ConsoleColor.Red);
            ConsoleEx.WriteLine($"Is System Versioned Enabled {tbl.IsSystemVersioned}", ConsoleColor.Blue);
            ConsoleEx.WriteLine($"History Table Name {tbl.HistoryTableSchema}.{tbl.HistoryTableName}", ConsoleColor.Blue);
            ConsoleEx.WriteLine($"Data Consistency Check {tbl.DataConsistencyCheck}", ConsoleColor.Blue);

            ConsoleEx.WriteLine("Let's make some updates - change the email address for the first record and see the result", ConsoleColor.Red);

            // The update is now; the address as it was at midnight comes from the history table
            var ds = db.ExecuteWithResults("UPDATE [Person].[EmailAddress] SET [EmailAddress] = 'test0@adventure-works.com' " +
                                           "WHERE BusinessEntityID=1 AND EmailAddressID=1; " +
                                           "DECLARE @Now AS DATE = CAST(SYSUTCDATETIME() AS DATE);" +
                                           "SELECT EmailAddress FROM [Person].[EmailAddress] FOR SYSTEM_TIME AS OF @Now WHERE BusinessEntityID=1 AND EmailAddressID=1");

            ConsoleEx.WriteLine($"The Email Address at midnight (UTC) was {ds.Tables[0].Rows[0][0]}");
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
