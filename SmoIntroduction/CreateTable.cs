using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SmoIntroduction
{


    internal static class CreateTable
    {

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
            Console.WriteLine("Connected");
            //Create the server object
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");
            //Create the database object
            var db = server.Databases[databaseName];


            var schemaName = ConfigurationManager.AppSettings["C_TEST_SCHEMA"];
            var tableName = ConfigurationManager.AppSettings["C_TEST_TABLE"];
            //
            //Create the schema if not exists
            //
            if (db.Schemas.Contains(schemaName) == false)
            {
                var hr = new Schema(db, schemaName);
                db.Schemas.Add(hr);
                db.Schemas[schemaName].Create();
            }

            Console.WriteLine("Create the schema object - if not exists");

            //
            //Drop the table if exists
            //
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine("Droping the table if exists");


            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            //
            var tbl = new Table(db, tableName, schemaName)
            {
                IsMemoryOptimized = false
            };
            // 
            //tbl.IsMemoryOptimized = true;
            //tbl.Durability = DurabilityType.SchemaAndData;

            // Add the identity column
            var col = new Column(tbl, @"ID", DataType.Int)
            {
                Nullable = false,
                Identity = true,
                IdentitySeed = 1,
                IdentityIncrement = 1
            };
            tbl.Columns.Add(col);



            var idx = new Index(tbl, $@"PK_{tableName}")
            {
                IsClustered = true,
                IsUnique = true,
                IndexKeyType = IndexKeyType.DriPrimaryKey
            };
            idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
            tbl.Indexes?.Add(idx);


            // Add the varchar column
            col = new Column(tbl, @"Name", DataType.VarChar(128)) { DataType = { MaximumLength = 128 } };
            col.AddDefaultConstraint(null);
            col.DefaultConstraint.Text = "''";
            col.Nullable = false;
            tbl.Columns.Add(col);

            // Add the datetime column
            col = new Column(tbl, @"Date", DataType.DateTime);
            tbl.Columns.Add(col);
            col.Nullable = false;

            Console.WriteLine($"Adding the table columns");
            // Create the table
            tbl.Create();

            Console.WriteLine($"Create the table on SQL Server {schemaName}.{tableName}");



            var sb = new StringBuilder();


            //Scripter scrp = new Scripter(server);
            //scrp.Options.ScriptDrops = false;
            //scrp.Options.WithDependencies = true;
            //scrp.Options.Indexes = true;             // To include indexes  
            //scrp.Options.DriAllConstraints = true;   // to include referential constraints in the script  

            //StringCollection sc = scrp.Script(new Urn[] { tbl.Urn });
            //foreach (string st in sc)
            //{
            //    sb.Append(st);
            //    sb.Append(CNewline);
            //}


            Console.WriteLine($"Make T-SQL script to create table {schemaName}.{tableName}");



            var coll = tbl.Script(MakeOptions());
            foreach (var str in coll)
            {
                sb.Append(str);
                sb.AppendLine();
            }

            var fileName = $"{tableName}{DateTime.Now:yyyy_mm_dd_HH_mm_ss}.txt";
            if (File.Exists(fileName))
                File.Delete(fileName);
            File.WriteAllText(fileName, sb.ToString());
            // start notepad and disply the configuration
            Process.Start(fileName);


            if (cnn.IsOpen)
                cnn.Disconnect();
            cnn = null;
            db = null;
            server = null;

            Console.WriteLine($"Press any key to exit...");
            Console.ReadLine();
        }

        public static ScriptingOptions MakeOptions()
        {
            ScriptingOptions o = null;
            try
            {
                o = new ScriptingOptions
                {
                    AllowSystemObjects = false,
                    AnsiFile = true,
                    AppendToFile = false,
                    AnsiPadding = true,


                    ClusteredIndexes = true,

                    DriIndexes = true,
                    DriClustered = true,
                    DriNonClustered = true,
                    DriAllConstraints = true,
                    DriAllKeys = true,
                    Default = true,
                    DriAll = true,

                    ExtendedProperties = true,
                    EnforceScriptingOptions = true,

                    IncludeIfNotExists = true,
                    Indexes = true,
                    IncludeHeaders = true,
                    IncludeDatabaseContext = true,



                    NoCommandTerminator = false,


                    Permissions = true,

                    SchemaQualify = true,
                    SchemaQualifyForeignKeysReferences = true,


                    NonClusteredIndexes = true,
                    NoCollation = false,
                    NoExecuteAs = true,


                    ScriptBatchTerminator = true,


                    WithDependencies = true
                };



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            return o;
        }


    }
}
