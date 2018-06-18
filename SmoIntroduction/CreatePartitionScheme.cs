using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Converter.Extension;

namespace SmoIntroduction
{
    class CreatePartitionScheme
    {


        // Based on Microsoft's example https://docs.microsoft.com/en-us/sql/t-sql/statements/create-partition-scheme-transact-sql?view=sql-server-2017

        static void Main(string[] args)
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            ServerConnection cnn;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
            }

            cnn.Connect();
            Console.WriteLine("Connected");
            //Create the server object
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");


            const string dataBaseName = @"TestPartition";
            try
            {

                //Drop the database if exists
                server.Databases[dataBaseName]?.Drop();

                //Create TestPartition database
                Console.WriteLine($"Creating the database{dataBaseName}");
                var db = new Database(server, dataBaseName);
                db.Create();

                // Adding four file groups and corespodenting files
                Console.WriteLine("Adding four file groups");
                for (var i = 1; i < 5; i++)
                {
                    db.FileGroups.Add(new FileGroup(db, $"test{i}fg"));
                    db.FileGroups[i].Files.Add(new DataFile(db.FileGroups[i], $"test{i}datafile",
                        $"{server.Information.MasterDBPath}\\{dataBaseName}_{i}.mdf"));
                }
                // Actually added
                db.Alter();



                Console.WriteLine("Creating the partition function");
                //Create the partition function
                var partitionFunction = new PartitionFunction(db, "myRangePF1");

                partitionFunction.PartitionFunctionParameters.Add(
                    new PartitionFunctionParameter(partitionFunction, DataType.Int));
                partitionFunction.RangeType = RangeType.Left;
                partitionFunction.RangeValues = new object[] { 1, 100, 1000 };
                partitionFunction.Create();

                // Filegroup  test1fg    test2fg                    test3fg                       test4fg
                // Partition  1          2                          3                             4
                // Values     col1 <= 1  col1 > 1 AND col1 <= 100   col1 > 100 AND col1 <= 1000   col1 > 1000

                Console.WriteLine("Creating the partition scheme");
                var partitionScheme = new PartitionScheme(db, "myRangePS1") { PartitionFunction = "myRangePF1" };
                for (var i = 1; i < 5; i++)
                    partitionScheme.FileGroups.Add($"test{i}fg");

                partitionScheme.Create();


                Console.WriteLine("Creating the table");
                var table = new Table(db, "TestTable");
                table.Columns.Add(new Column(table, "col1", DataType.Int));
                table.PartitionScheme = "myRangePS1";
                table.PartitionSchemeParameters.Add(new PartitionSchemeParameter(table, "col1"));
                table.Create();

                //Insert a few records into newly create table
                db.ExecuteNonQuery(@"INSERT INTO DBO.TESTTABLE
                        VALUES (0), (1), (100), (200), (3000);");


                // Exemine sys.dm_db_partition_stats
                var dataset = db.ExecuteWithResults(
                    @"SELECT partition_number,row_count
                       FROM sys.dm_db_partition_stats
                       WHERE object_id = OBJECT_ID('DBO.TESTTABLE');");

                foreach (DataRow row in dataset.Tables[0].Rows)
                {
                    Console.WriteLine($"Partition {row["partition_number"]} has {row["row_count"]} rows");
                }
                db = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message)));

            }


            if (cnn.IsOpen)
                cnn.Disconnect();
            cnn = null;
          
            server = null;
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();




        }
    }
}
