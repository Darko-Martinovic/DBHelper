using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SmoIntroduction
{


    class CreateTable
    {
        private const string C_DATABASENAME = "AdventureWorks2014";
        private const string C_NEWLINE = "\r\n";
        private const string C_TEST_TABLE = "TestTable";
        private const string C_TEST_SCHEMA = "HumanResources";

        //Added for Memory-optimized tables
        private const string C_FILE_GROUP = "mofg";
        private const string C_FILE_NAME = "mofile";
        private const string C_MO_PATH = @"C:\HKDATAAW";
        private const string C_SERVER_VERSION = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545


        static void Main(string[] args)
        {
            ServerConnection cnn = new ServerConnection();
            cnn.Connect();
            Console.Write("Connected" + C_NEWLINE);
            //Create the server object
            Server server = new Server(cnn);
            Console.Write("Create the server object - default instance" + C_NEWLINE);
            //Create the database object
            Database db = server.Databases[C_DATABASENAME];

            //
            // Only for SQL Server version 2016 SP1
            // Add MEMORY OPTIMIZED FILE GROUP AND FILE 
            if (server.Version >= new Version(C_SERVER_VERSION))
            {
                Console.Write("Add support for memory optimized tables" + C_NEWLINE);
                // First check if there is already memory optimized file group 
                bool isMemoryOptimizedFileGropuExists = false;

                foreach (FileGroup f in db.FileGroups)
                {
                    if (f.FileGroupType == FileGroupType.MemoryOptimizedDataFileGroup)
                    {
                        isMemoryOptimizedFileGropuExists = true;
                        break;
                    }
                }
                if (isMemoryOptimizedFileGropuExists == false)
                { 
                    // If memory optimized file group does not exists - create 
                    if ( db.FileGroups.Contains(C_FILE_GROUP) == false)
                    {
                        // C_FILE_GROUP is constant defined above as 
                        // private const string C_FILE_GROUP = "mofg";
                        FileGroup mo = new FileGroup(db, C_FILE_GROUP, FileGroupType.MemoryOptimizedDataFileGroup);
                        db.FileGroups.Add(mo);
                        db.FileGroups[C_FILE_GROUP].Create();
                    }
                    // If the file for memory optimized file group does not exists - create 
                    if (db.FileGroups[C_FILE_GROUP].Files.Contains(C_FILE_NAME) == false)
                    {
                        // C_MO_PATH is the constant defined as private const string C_MO_PATH = @"C:\HKDATAAW";
                        // C_FILE_NAME is the constant defined as private const string C_FILE_NAME = "mofile";
                        // C_FILE_GROUP is the constant defined as private const string C_FILE_GROUP = "mofg";
                        string path = C_MO_PATH;
                        // Create the file ( the container ) 
                        DataFile df = new DataFile(db.FileGroups[C_FILE_GROUP], C_FILE_NAME,path);
                        // Add the container to the memory optimized file group
                        db.FileGroups[C_FILE_GROUP].Files.Add(df);
                        // Actually create. Now it exists in database
                        db.FileGroups[C_FILE_GROUP].Files[C_FILE_NAME].Create();

                    }
                }
            }
            //
            // end database operation - adding memory optimized file group 
            //



            //
            //Create the schema if not exists
            //
            if (db.Schemas.Contains(C_TEST_SCHEMA) == false)
            {
                Schema hr = new Schema(db, C_TEST_SCHEMA);
                db.Schemas.Add(hr);
                db.Schemas[C_TEST_SCHEMA].Create();
            }

            Console.Write("Create the schema object - if not exists" + C_NEWLINE);

            //
            //Drop the table if exists
            //
            if (db.Tables.Contains(C_TEST_TABLE, C_TEST_SCHEMA))
                db.Tables[C_TEST_TABLE, C_TEST_SCHEMA].Drop();
            Console.Write("Droping the table if exists" + C_NEWLINE);


            Console.Write("Create the table object " + C_TEST_SCHEMA + "." + C_TEST_TABLE + C_NEWLINE);

            //
            // Create a new table object
            //
            Table tbl = new Table(db, C_TEST_TABLE, C_TEST_SCHEMA);
            tbl.IsMemoryOptimized = false;
            // 
            tbl.IsMemoryOptimized = true;
            tbl.Durability = DurabilityType.SchemaAndData;

            // Add the identity column
            Column col = new Column(tbl, @"ID", DataType.Int);
            tbl.Columns.Add(col);
            col.Nullable = false;
            col.Identity = true;
            col.IdentitySeed = 1;
            col.IdentityIncrement = 1;
            

            // Add the primary key index
            if (tbl.IsMemoryOptimized == false)
            {
                Index idx = new Index(tbl, @"PK_" + C_TEST_TABLE);
                tbl.Indexes.Add(idx);
                idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
                idx.IsClustered = true;
                idx.IsUnique = true;
                idx.IndexKeyType = IndexKeyType.DriPrimaryKey;
            }
            else
            {
                Index idx = new Index(tbl, @"PK_" + C_TEST_TABLE);
                idx.IndexType = IndexType.NonClusteredIndex;
                idx.IndexKeyType = IndexKeyType.DriPrimaryKey;
                tbl.Indexes.Add(idx);
                idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
            }

            // Add the nvarchar column
            col = new Column(tbl, @"Name", DataType.VarChar(128));
            tbl.Columns.Add(col);
            col.DataType.MaximumLength = 128;
            col.AddDefaultConstraint(null);
            col.DefaultConstraint.Text = "''";
            col.Nullable = false;

            // Add the datetime column
            col = new Column(tbl, @"Date", DataType.DateTime);
            tbl.Columns.Add(col);
            col.Nullable = false;

            Console.Write("Adding the table columns " + C_NEWLINE);
            // Create the table
            tbl.Create();

            Console.Write("Create the table on SQL Server " + C_TEST_SCHEMA + "." + C_TEST_TABLE + C_NEWLINE);
           

            if (tbl != null)
            {
                Console.Write("Make T-SQL script to create table " + C_TEST_SCHEMA + "." + C_TEST_TABLE + C_NEWLINE);


                StringBuilder sb = new StringBuilder();

                StringCollection coll = tbl.Script(MakeOptions());
                foreach (string str in coll)
                {
                    sb.Append(str);
                    sb.Append(C_NEWLINE);
                }

                string fileName = C_TEST_TABLE + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".txt";
                if (File.Exists(fileName))
                    File.Delete(fileName);
                File.WriteAllText(fileName, sb.ToString());
                // start notepad and disply the configuration
                Process.Start(fileName);

            }
            if (cnn.IsOpen)
            {
                cnn.Disconnect();
                cnn = null;
            }
            if (db != null)
                db = null;
            if (server != null)
                server = null;

            Console.Write("Press any key to exit..." + C_NEWLINE);
            Console.ReadLine();
        }

        private static ScriptingOptions MakeOptions()
        {
            ScriptingOptions o = new ScriptingOptions();
            try
            {
                o.AllowSystemObjects = false;
                o.AnsiFile = true;
                o.AppendToFile = false;
                o.AnsiPadding = true;

            
                o.ClusteredIndexes = true;

                o.DriIndexes = true;
                o.DriClustered = true;
                o.DriNonClustered = true;
                o.DriAllConstraints = true;
                o.DriAllKeys = true;
                o.Default = true;
                o.DriAll = true;

                o.ExtendedProperties = true;
                o.EnforceScriptingOptions = true;

                o.IncludeIfNotExists = true;
                o.Indexes = true;
                o.IncludeHeaders = true;
                o.IncludeDatabaseContext = true;



                o.NoCommandTerminator = false;
              
               
                o.Permissions = true;
               
                o.SchemaQualify = true;
                o.SchemaQualifyForeignKeysReferences = true;
               
                
                o.NonClusteredIndexes = true;
                o.NoCollation = false;
                o.NoExecuteAs = true;
              
              
                o.ScriptBatchTerminator = true;

                        
                o.WithDependencies = true;


                   
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
