using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace SmoIntroduction
{
    public class CreateSysVerTempTables
    {
        private const string CServerVersion = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545

        static void Main(string[] args)
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            ServerConnection cnn;
            string databaseName;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
                // Read the database name from app.config
                databaseName = sqlConnection.Database;
            }
            cnn.Connect();
            var server = new Server(cnn);

            if (server.Version <= new Version(CServerVersion))
            {
                ConsoleEx.WriteLine("Only supported for SQL 2016+");
                Console.ReadLine();
            }
            try
            {

                var db = server.Databases[databaseName];
                //Person.EmailAddress
                var tbl = db.Tables["EmailAddress", "Person"];

                // --== Let's define the first column 'ValidFrom' ==---
                //      the equivalent T-SQL would be 'ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL'
                var col = new Column(tbl, "ValidForm")
                {
                    DataType = DataType.DateTime2(7),
                    //IsHidden = true,
                    //GeneratedAlwaysType = GeneratedAlwaysType.AsRowStart,
                    Nullable = false,

                };
                col.AddDefaultConstraint("DfValidFrom");
                col.DefaultConstraint.Text = "'1991-01-01 00:00:00.0000000'";

                tbl.Columns.Add(col);


                // --== Let's define the column 'ValidTo' ==---
                var col2 = new Column(tbl, "ValidTo")
                {
                    DataType = DataType.DateTime2(7),
                    //IsHidden = true,
                    //GeneratedAlwaysType = GeneratedAlwaysType.AsRowEnd,
                    Nullable = false,


                };
                col2.AddDefaultConstraint("DfValidTo");
                col2.DefaultConstraint.Text = "'9999-12-31 23:59:59.9999999'";
                tbl.Columns.Add(col2);
            

                // --== Let's define the period ==---
                tbl.Alter();
                tbl.AddPeriodForSystemTime(col.Name, col2.Name, true);
                tbl.Alter();


                tbl.IsSystemVersioned = true;
                tbl.HistoryTableSchema = tbl.Schema;
                tbl.HistoryTableName = $"{tbl.Name}_History";
                tbl.DataConsistencyCheck = true;
                tbl.Alter();

                ConsoleEx.WriteLine("Let's Examine the Table ", ConsoleColor.Red);
                ConsoleEx.WriteLine($"Is System Versioned Enabled {tbl.IsSystemVersioned}", ConsoleColor.Blue);
                ConsoleEx.WriteLine($"History Table Name {tbl.HistoryTableSchema}.{tbl.HistoryTableName}", ConsoleColor.Blue);
                ConsoleEx.WriteLine($"Data Consistency Check {tbl.DataConsistencyCheck}", ConsoleColor.Blue);


                ConsoleEx.WriteLine("Let's make some updates - change the email address for the first record and see the result", ConsoleColor.Red);


                var ds = db.ExecuteWithResults("UPDATE [Person].[EmailAddress] SET [EmailAddress] = 'test0@adventure-works.com' " +
                                               "WHERE BusinessEntityID=1 AND EmailAddressID=1; " +
                                               "DECLARE @Now AS DATE = CAST(GETDATE() AS DATE);" +
                "SELECT EmailAddress FROM [Person].[EmailAddress] FOR SYSTEM_TIME AS OF @Now WHERE BusinessEntityID=1 AND EmailAddressID=1");


                ConsoleEx.WriteLine($"The Email Address Is {ds.Tables[0].Rows[0][0]}");

                Console.ReadLine();





            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLine(ex.ToString(), ConsoleColor.Red);
                Console.ReadLine();
            }

        }
    }
}
