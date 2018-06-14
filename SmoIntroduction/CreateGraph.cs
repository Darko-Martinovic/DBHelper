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
    public class CreateGraph
    {
        private const string
            CServerVersion =
                "14.0.1000"; // https://support.microsoft.com/en-us/help/4047329/sql-server-2017-build-versions



        /// <summary>
        /// Based on Microsoft example https://docs.microsoft.com/en-us/sql/relational-databases/graphs/sql-graph-sample?view=sql-server-2017
        /// </summary>
        /// <param name="args"></param>
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

            //
            // Only for SQL Server version 2017
            if (server.Version < new Version(CServerVersion))
            {
                Console.WriteLine("Only for SQL Server 2017 and above");
                Console.ReadLine();
                return;

            }
            //Create NODE tables
            CreateTablePerson(db);
            CreateTableRestaurant(db);
            CreateTableCity(db);

            //Create EDGE tables
            CreateLikes(db);
            CreateFriendOf(db);
            CreateLivesIn(db);
            CreateLocatedIn(db);

            //Insert data
            try
            {

                Console.WriteLine("Insert data");

                db.ExecuteNonQuery(
                    @"-- Insert data into node tables. Inserting into a node table is same as inserting into a regular table
INSERT INTO Person VALUES (1,'John');
INSERT INTO Person VALUES (2,'Mary');
INSERT INTO Person VALUES (3,'Alice');
INSERT INTO Person VALUES (4,'Jacob');
INSERT INTO Person VALUES (5,'Julie');

INSERT INTO Restaurant VALUES (1,'Taco Dell','Bellevue');
INSERT INTO Restaurant VALUES (2,'Ginger and Spice','Seattle');
INSERT INTO Restaurant VALUES (3,'Noodle Land', 'Redmond');

INSERT INTO City VALUES (1,'Bellevue','wa');
INSERT INTO City VALUES (2,'Seattle','wa');
INSERT INTO City VALUES (3,'Redmond','wa');

-- Insert into edge table. While inserting into an edge table, 
-- you need to provide the $node_id from $from_id and $to_id columns.
INSERT INTO likes VALUES ((SELECT $node_id FROM Person WHERE id = 1), 
       (SELECT $node_id FROM Restaurant WHERE id = 1),9);
INSERT INTO likes VALUES ((SELECT $node_id FROM Person WHERE id = 2), 
      (SELECT $node_id FROM Restaurant WHERE id = 2),9);
INSERT INTO likes VALUES ((SELECT $node_id FROM Person WHERE id = 3), 
      (SELECT $node_id FROM Restaurant WHERE id = 3),9);
INSERT INTO likes VALUES ((SELECT $node_id FROM Person WHERE id = 4), 
      (SELECT $node_id FROM Restaurant WHERE id = 3),9);
INSERT INTO likes VALUES ((SELECT $node_id FROM Person WHERE id = 5), 
      (SELECT $node_id FROM Restaurant WHERE id = 3),9);

INSERT INTO livesIn VALUES ((SELECT $node_id FROM Person WHERE id = 1),
      (SELECT $node_id FROM City WHERE id = 1));
INSERT INTO livesIn VALUES ((SELECT $node_id FROM Person WHERE id = 2),
      (SELECT $node_id FROM City WHERE id = 2));
INSERT INTO livesIn VALUES ((SELECT $node_id FROM Person WHERE id = 3),
      (SELECT $node_id FROM City WHERE id = 3));
INSERT INTO livesIn VALUES ((SELECT $node_id FROM Person WHERE id = 4),
      (SELECT $node_id FROM City WHERE id = 3));
INSERT INTO livesIn VALUES ((SELECT $node_id FROM Person WHERE id = 5),
      (SELECT $node_id FROM City WHERE id = 1));

INSERT INTO locatedIn VALUES ((SELECT $node_id FROM Restaurant WHERE id = 1),
      (SELECT $node_id FROM City WHERE id =1));
INSERT INTO locatedIn VALUES ((SELECT $node_id FROM Restaurant WHERE id = 2),
      (SELECT $node_id FROM City WHERE id =2));
INSERT INTO locatedIn VALUES ((SELECT $node_id FROM Restaurant WHERE id = 3),
      (SELECT $node_id FROM City WHERE id =3));

-- Insert data into the friendof edge.
INSERT INTO friendof VALUES ((SELECT $NODE_ID FROM person WHERE ID = 1), (SELECT $NODE_ID FROM person WHERE ID = 2));
INSERT INTO friendof VALUES ((SELECT $NODE_ID FROM person WHERE ID = 2), (SELECT $NODE_ID FROM person WHERE ID = 3));
INSERT INTO friendof VALUES ((SELECT $NODE_ID FROM person WHERE ID = 3), (SELECT $NODE_ID FROM person WHERE ID = 1));
INSERT INTO friendof VALUES ((SELECT $NODE_ID FROM person WHERE ID = 4), (SELECT $NODE_ID FROM person WHERE ID = 2));
INSERT INTO friendof VALUES ((SELECT $NODE_ID FROM person WHERE ID = 5), (SELECT $NODE_ID FROM person WHERE ID = 4));

");

                using (var dataset = db.ExecuteWithResults(
                    @"-- Find Restaurants that John likes
SELECT Restaurant.name
FROM Person, likes, Restaurant
WHERE MATCH (Person-(likes)->Restaurant)
AND Person.name = 'John';
-- Find Restaurants that John's friends like
SELECT Restaurant.name 
FROM Person person1, Person person2, likes, friendOf, Restaurant
WHERE MATCH(person1-(friendOf)->person2-(likes)->Restaurant)
AND person1.name='John';

-- Find people who like a restaurant in the same city they live in
SELECT Person.name
FROM Person, likes, Restaurant, livesIn, City, locatedIn
WHERE MATCH (Person-(likes)->Restaurant-(locatedIn)->City AND Person-(livesIn)->City);
"))
                {

                    Console.WriteLine("-----------------------------------------------");
                    Console.WriteLine("Find Restaurants that John likes");
                    Console.WriteLine("-----------------------------------------------");
                    foreach (DataRow r in dataset.Tables[0].Rows)
                        Console.WriteLine("\t" + r[0]);
                    Console.WriteLine("-----------------------------------------------");
                    Console.WriteLine("Find Restaurants that John's friends like");
                    Console.WriteLine("-----------------------------------------------");
                    foreach (DataRow r in dataset.Tables[1].Rows)
                        Console.WriteLine("\t" + r[0]);
                    Console.WriteLine("-----------------------------------------------");
                    Console.WriteLine("Find people who like a restaurant in the same city they live in");
                    Console.WriteLine("-----------------------------------------------");
                    foreach (DataRow r in dataset.Tables[2].Rows)
                        Console.WriteLine("\t" + r[0]);
                    Console.WriteLine("-----------------------------------------------");
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine(string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message)));

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                cnn = null;
                db = null;
                server = null;
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();


        }

        private static void CreateLocatedIn(Database db)
        {
            const string tableName = "locatedIn";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set isEdge = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsEdge = true

            };
            tbl.Create();
            tbl = null;


        }

        private static void CreateLivesIn(Database db)
        {
            const string tableName = "livesIn";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set isEdge = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsEdge = true

            };
            tbl.Create();
            tbl = null;


        }

        private static void CreateFriendOf(Database db)
        {
            const string tableName = "friendOf";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set isEdge = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsEdge = true

            };
            tbl.Create();
            tbl = null;

        }

        private static void CreateLikes(Database db)
        {
            const string tableName = "likes";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set isEdge = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsEdge = true

            };
            var col = new Column(tbl, @"raiting", DataType.Int);
            tbl.Columns.Add(col);
            tbl.Create();
            tbl = null;
            col = null;

        }

        private static void CreateTableCity(Database db)
        {
            //
            //Drop the table if exists
            //
            const string tableName = "City";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set IsNode = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsNode = true
            };
            // Add the identity column
            var col = new Column(tbl, @"ID", DataType.Int)
            {
                Nullable = false,
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


            // Add the varchar column name
            col = new Column(tbl, @"name", DataType.VarChar(100)) { DataType = { MaximumLength = 100 } };
            tbl.Columns.Add(col);

            // Add the varchar column city
            col = new Column(tbl, @"stateName", DataType.VarChar(100)) { DataType = { MaximumLength = 100 } };
            tbl.Columns.Add(col);
            tbl.Create();
            tbl = null;
            col = null;


        }

        private static void CreateTableRestaurant(Database db)
        {
            //
            //Drop the table if exists
            //

            const string tableName = "Restaurant";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();


            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set IsNode = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsNode = true
            };
            // Add the identity column
            var col = new Column(tbl, @"ID", DataType.Int)
            {
                Nullable = false,
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


            // Add the varchar column name
            col = new Column(tbl, @"name", DataType.VarChar(100)) { DataType = { MaximumLength = 100 } };
            tbl.Columns.Add(col);

            // Add the varchar column city
            col = new Column(tbl, @"city", DataType.VarChar(100)) { DataType = { MaximumLength = 100 } };
            tbl.Columns.Add(col);
            tbl.Create();
            tbl = null;
            col = null;
            idx = null;


        }

        private static void CreateTablePerson(Database db)
        {
            //
            //Drop the table if exists
            //
            const string tableName = "Person";
            const string schemaName = "dbo";

            Console.WriteLine("Droping the table if exists");
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            Console.WriteLine($"Create the table object {schemaName}.{tableName}");

            //
            // Create a new table object
            // Set IsNode = true
            var tbl = new Table(db, tableName, schemaName)
            {
                IsNode = true
            };
            // Add the identity column
            var col = new Column(tbl, @"ID", DataType.Int)
            {
                Nullable = false,
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
            col = new Column(tbl, @"name", DataType.VarChar(128)) { DataType = { MaximumLength = 100 } };
            tbl.Columns.Add(col);
            tbl.Create();
            tbl = null;
            idx = null;
            col = null;

        }
    }
}