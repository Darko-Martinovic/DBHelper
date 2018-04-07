using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;

namespace SmoIntroduction
{
   

    public class CreateMOTable
    {

     
        private const string C_NEWLINE = "\r\n";
       

        //Added for Memory-optimized tables
        private const string C_FILE_GROUP = "mofg";
        private const string C_FILE_NAME = "mofile";
      
        private const string C_SERVER_VERSION = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545

        static void Main(string[] args)
        {

            String connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            ServerConnection cnn = new ServerConnection(sqlConnection);
            // Read the database name from app.config
            string databaseName = sqlConnection.Database;

                   
            cnn.Connect();
            Console.Write("Connected" + C_NEWLINE);
            //Create the server object
            Server server = new Server(cnn);
            Console.Write("Create the server object" + C_NEWLINE);
            //Create the database object
            Database db = server.Databases[databaseName];

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
                    if (db.FileGroups.Contains(C_FILE_GROUP) == false)
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
                        // C_MO_PATH is the constant defined in app.config = @"C:\HKDATAAW";
                        // C_FILE_NAME is the constant defined as private const string C_FILE_NAME = "mofile";
                        // C_FILE_GROUP is the constant defined as private const string C_FILE_GROUP = "mofg";
                        string path = ConfigurationManager.AppSettings["C_MO_PATH"];
                        // Create the file ( the container ) 
                        DataFile df = new DataFile(db.FileGroups[C_FILE_GROUP], C_FILE_NAME, path);
                        // Add the container to the memory optimized file group
                        db.FileGroups[C_FILE_GROUP].Files.Add(df);
                        // Actually create. Now it exists in the database
                        try
                        {
                            db.FileGroups[C_FILE_GROUP].Files[C_FILE_NAME].Create();
                        }
                        catch (Exception ex)
                        {
                            Console.Write(ex.Message);
                            Console.Write("Press any key to exit..." + C_NEWLINE);
                            Console.ReadLine();
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
            string schemaName = ConfigurationManager.AppSettings["C_MO_TEST_SCHEMA"];
            if (db.Schemas.Contains(schemaName) == false)
            {
                Schema hr = new Schema(db, schemaName);
                db.Schemas.Add(hr);
                db.Schemas[schemaName].Create();
            }

            Console.Write("Create the schema object - if not exists" + C_NEWLINE);

            //
            //Drop the table if exists
            //
            string tableName = ConfigurationManager.AppSettings["C_MO_TEST_TABLE"];
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.Write("Droping the table if exists" + C_NEWLINE);


            Console.Write("Create the table object " + schemaName + "." + tableName + C_NEWLINE);

            //
            // Create a new table object
            //
            Table tbl = new Table(db, tableName, schemaName);

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
           
            Index idx = new Index(tbl, @"PK_" + tableName);
            idx.IndexType = IndexType.NonClusteredIndex;
            idx.IndexKeyType = IndexKeyType.DriPrimaryKey;
            tbl.Indexes.Add(idx);
            idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
         

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

            Console.Write("Create the table on SQL Server " + schemaName + "." + tableName + C_NEWLINE);

            StringBuilder sb = new StringBuilder();


           


            if (tbl != null)
            {
                Console.Write("Make T-SQL script to create table " + schemaName + "." + tableName + C_NEWLINE);



                StringCollection coll = tbl.Script(CreateTable.MakeOptions());
                foreach (string str in coll)
                {
                    sb.Append(str);
                    sb.Append(C_NEWLINE);
                }

                string fileName = tableName + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".txt";
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



    }
    }
