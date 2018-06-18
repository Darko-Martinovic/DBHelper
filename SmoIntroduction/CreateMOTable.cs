using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Converter.Extension;

namespace SmoIntroduction
{


    public class CreateMoTable
    {

        //Hard coded the file group name and the container name
        private const string CFileGroup = "mofg";
        private const string CFileName = "mofile";

        private const string CServerVersion = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545

        static void Main(string[] args)
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            ServerConnection cnn;
            string databaseName;
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
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
            try
            {

                var db = server.Databases[databaseName];

                //
                // Only for SQL Server version 2016 SP1
                // Add MEMORY OPTIMIZED FILE GROUP AND FILE 
                if (server.Version >= new Version(CServerVersion))
                {
                    Console.WriteLine("Add support for memory optimized tables");
                    // First check if there is already memory optimized file group 
                    var isMemoryOptimizedFileGropuExists = false;

                    foreach (FileGroup f in db.FileGroups)
                    {
                        if (f.FileGroupType != FileGroupType.MemoryOptimizedDataFileGroup) continue;
                        isMemoryOptimizedFileGropuExists = true;
                        break;
                    }
                    if (isMemoryOptimizedFileGropuExists == false)
                    {
                        // If memory optimized file group does not exists - create 
                        if (db.FileGroups.Contains(CFileGroup) == false)
                        {
                            // CFileGroup is constant defined above as 
                            // private const string CFileGroup = "mofg";
                            var mo = new FileGroup(db, CFileGroup, FileGroupType.MemoryOptimizedDataFileGroup);
                            db.FileGroups.Add(mo);
                            db.FileGroups[CFileGroup].Create();
                        }
                        // If the file for memory optimized file group does not exists - create 
                        if (db.FileGroups[CFileGroup].Files.Contains(CFileName) == false)
                        {
                            // C_MO_PATH is the constant defined in app.config ;
                            // CFileName is the constant defined as private const string CFileName = "mofile";
                            // CFileGroup is the constant defined as private const string CFileGroup = "mofg";
                            var path = ConfigurationManager.AppSettings["C_MO_PATH"] + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
                            // Create the file ( the container ) 
                            var df = new DataFile(db.FileGroups[CFileGroup], CFileName, path);
                            // Add the container to the memory optimized file group
                            db.FileGroups[CFileGroup].Files.Add(df);
                            // Actually create. Now it exists in the database
                            try
                            {
                                db.FileGroups[CFileGroup].Files[CFileName].Create();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine("Press any key to exit...");
                                Console.ReadLine();
                                return;
                            }

                        }
                    }
                }
                //
                // end database operation - adding memory optimized file group 
                //

                //
                //Create the schema if not exists
                //
                var schemaName = ConfigurationManager.AppSettings["C_MO_TEST_SCHEMA"];
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
                var tableName = ConfigurationManager.AppSettings["C_MO_TEST_TABLE"];
                if (db.Tables.Contains(tableName, schemaName))
                    db.Tables[tableName, schemaName].Drop();
                Console.WriteLine("Droping the table if exists");


                Console.WriteLine($"Create the table object {schemaName}.{tableName}");

                //
                // Create a new table object
                //
                var tbl = new Table(db, tableName, schemaName)
                {

                    // 
                    IsMemoryOptimized = true,
                    Durability = DurabilityType.SchemaAndData
                };


                // Add the identity column
                var col = new Column(tbl, @"ID", DataType.Int)
                {
                    Nullable = false,
                    Identity = true,
                    IdentitySeed = 1,
                    IdentityIncrement = 1
                };
                tbl.Columns.Add(col);


                // Add the primary key index

                var idx = new Index(tbl, $@"PK_{tableName}")
                {
                    IndexType = IndexType.NonClusteredHashIndex,
                    BucketCount = 128,
                    IndexKeyType = IndexKeyType.DriPrimaryKey
                };
                //idx.IndexType = IndexType.NonClusteredIndex;

                tbl.Indexes.Add(idx);
                idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));


                // Add the varchar column
                col = new Column(tbl, @"Name", DataType.VarChar(128)) { DataType = { MaximumLength = 128 } };
                col.AddDefaultConstraint(null);
                col.DefaultConstraint.Text = "''";
                col.Nullable = false;
                tbl.Columns.Add(col);

                //Add range index 
                idx = new Index(tbl, @"NAME_" + tableName)
                {
                    IndexType = IndexType.NonClusteredIndex,
                    IndexKeyType = IndexKeyType.None
                };
                tbl.Indexes.Add(idx);
                idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));

                // Add the datetime column
                col = new Column(tbl, @"Date", DataType.DateTime);
                tbl.Columns.Add(col);
                col.Nullable = false;

                Console.WriteLine("Adding the table columns");
                // Create the table
                tbl.Create();

                Console.WriteLine($"Create the table on SQL Server {schemaName}.{tableName}");

                var sb = new StringBuilder();


                Console.WriteLine($"Make T-SQL script to create table {schemaName}.{tableName}");


                var coll = tbl.Script(CreateTable.MakeOptions());
                foreach (var str in coll)
                    sb.AppendLine(str);

                string fileName = $"{tableName}{DateTime.Now:yyyy_mm_dd_HH_mm_ss}.txt";
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

                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message)));

            }


        }



    }
}
