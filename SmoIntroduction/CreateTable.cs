﻿using Microsoft.SqlServer.Management.Common;
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


    public class CreateTable
    {
        private const string C_DATABASENAME = "AdventureWorks2014";
        private const string C_NEWLINE = "\r\n";
        private const string C_TEST_TABLE = "TestTable";
        private const string C_TEST_SCHEMA = "HumanResources";

       
        static void Main(string[] args)
        {
            ServerConnection cnn = new ServerConnection(new SqlConnection(ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString));
            cnn.Connect();
            Console.Write("Connected" + C_NEWLINE);
            //Create the server object
            Server server = new Server(cnn);
            Console.Write("Create the server object - default instance" + C_NEWLINE);
            //Create the database object
            Database db = server.Databases[C_DATABASENAME];

            


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
            //tbl.IsMemoryOptimized = true;
            //tbl.Durability = DurabilityType.SchemaAndData;

            // Add the identity column
            Column col = new Column(tbl, @"ID", DataType.Int);
            tbl.Columns.Add(col);
            col.Nullable = false;
            col.Identity = true;
            col.IdentitySeed = 1;
            col.IdentityIncrement = 1;
            

          
            Index idx = new Index(tbl, @"PK_" + C_TEST_TABLE);
            tbl.Indexes.Add(idx);
            idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
            idx.IsClustered = true;
            idx.IsUnique = true;
            idx.IndexKeyType = IndexKeyType.DriPrimaryKey;
            
           
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



            StringBuilder sb = new StringBuilder();


            //Scripter scrp = new Scripter(server);
            //scrp.Options.ScriptDrops = false;
            //scrp.Options.WithDependencies = true;
            //scrp.Options.Indexes = true;             // To include indexes  
            //scrp.Options.DriAllConstraints = true;   // to include referential constraints in the script  

            //StringCollection sc = scrp.Script(new Urn[] { tbl.Urn });
            //foreach (string st in sc)
            //{
            //    sb.Append(st);
            //    sb.Append(C_NEWLINE);
            //}


            if (tbl != null)
            {
                Console.Write("Make T-SQL script to create table " + C_TEST_SCHEMA + "." + C_TEST_TABLE + C_NEWLINE);



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

        public static ScriptingOptions MakeOptions()
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
