using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;

namespace SmoIntroduction
{


    class CreateTable
    {

        private const string C_DATABASENAME = "AdventureWorks2014";
        private const string C_NEWLINE = "\r\n";
        private const string C_TEST_TABLE = "TestTable";
        private const string C_TEST_SCHEMA = "HumanResources";

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


            //Create the schema if not exists
            if (db.Schemas.Contains(C_TEST_SCHEMA) == false)
            {
                db.Schemas[C_TEST_SCHEMA].Create();
            }
            Console.Write("Create the schema object - if not exists" + C_NEWLINE);

            //Drop the table if exists
            if (db.Tables.Contains(C_TEST_TABLE, C_TEST_SCHEMA))
            {
                db.Tables[C_TEST_TABLE, C_TEST_SCHEMA].Drop();
            }

            // Create a new table object
            Table tbl = new Table(db, C_TEST_TABLE, C_TEST_SCHEMA);

            // Add the identity column
            Column col = new Column(tbl, @"ID", DataType.Int);
            tbl.Columns.Add(col);
            col.Nullable = false;
            col.Identity = true;
            col.IdentitySeed = 1;
            col.IdentityIncrement = 1;

            // Add the primary key index
            Index idx = new Index(tbl, @"PK_" + C_TEST_TABLE);
            tbl.Indexes.Add(idx);
            idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
            idx.IsClustered = true;
            idx.IsUnique = true;
            idx.IndexKeyType = IndexKeyType.DriPrimaryKey;

            // Add the nvarchar column
            col = new Column(tbl, @"Name", DataType.NVarChar(1024));
            tbl.Columns.Add(col);
            col.DataType.MaximumLength = 128;
            col.AddDefaultConstraint(null);
            col.Nullable = false;

            // Add the datetime column
            col = new Column(tbl, @"Date", DataType.DateTime);
            tbl.Columns.Add(col);
            col.Nullable = false;

            // Create the table
            tbl.Create();

            if (cnn.IsOpen)
            {
                cnn.Disconnect();
                cnn = null;
            }

            if (server != null)
            {
                server = null;
            }
            Console.Write("Press any key to exit..." + C_NEWLINE);
            Console.ReadLine();
        }

    }
}
