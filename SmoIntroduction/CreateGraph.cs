using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Index = Microsoft.SqlServer.Management.Smo.Index;

namespace SmoIntroduction;

/// <summary>
/// Based on Microsoft example https://learn.microsoft.com/en-us/sql/relational-databases/graphs/sql-graph-sample
/// </summary>
public sealed class CreateGraph : IExample
{
    private const string
        CServerVersion =
            "14.0.1000"; // https://support.microsoft.com/en-us/help/4047329/sql-server-2017-build-versions

    public string Description => "Shows how to create NODE & EDGE tables";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();
        var databaseName = context.DatabaseName;

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            //Create the server object
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");
            //Create the database object
            var db = server.Databases[databaseName];

            //
            // Only for SQL Server version 2017
            if (server.Version < new Version(CServerVersion))
            {
                ConsoleEx.WriteLine("Only for SQL Server 2017 and above", ConsoleColor.Cyan);
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

            InsertData(db);

            using var dataset = db.ExecuteWithResults(
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
");

            Console.WriteLine("-----------------------------------------------");
            ConsoleEx.WriteLine("Find Restaurants that John likes", ConsoleColor.Cyan);
            Console.WriteLine("-----------------------------------------------");
            foreach (DataRow r in dataset.Tables[0].Rows)
                ConsoleEx.WriteLine("\t" + r[0], ConsoleColor.Yellow);
            Console.WriteLine("-----------------------------------------------");
            ConsoleEx.WriteLine("Find Restaurants that John's friends like", ConsoleColor.Cyan);
            Console.WriteLine("-----------------------------------------------");
            foreach (DataRow r in dataset.Tables[1].Rows)
                ConsoleEx.WriteLine("\t" + r[0], ConsoleColor.Yellow);
            Console.WriteLine("-----------------------------------------------");
            ConsoleEx.WriteLine("Find people who like a restaurant in the same city they live in", ConsoleColor.Cyan);
            Console.WriteLine("-----------------------------------------------");
            foreach (DataRow r in dataset.Tables[2].Rows)
                ConsoleEx.WriteLine("\t" + r[0], ConsoleColor.Yellow);
            Console.WriteLine("-----------------------------------------------");
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }

    private static void InsertData(Database db)
    {
        //Insert data
        ConsoleEx.WriteLine("Insert data", ConsoleColor.Yellow);
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
    }

    private static void CreateEdge(Database db, string tableName, string? ratingColumn = null)
    {
        const string schemaName = "dbo";

        ConsoleEx.WriteLine("Droping the table if exists", ConsoleColor.Red);
        if (db.Tables.Contains(tableName, schemaName))
            db.Tables[tableName, schemaName].Drop();
        ConsoleEx.WriteLine($"Create the table object {schemaName}.{tableName}", ConsoleColor.Cyan);

        //
        // Create a new table object
        // Set isEdge = true
        var tbl = new Table(db, tableName, schemaName)
        {
            IsEdge = true
        };
        if (ratingColumn != null)
            tbl.Columns.Add(new Column(tbl, ratingColumn, DataType.Int));
        tbl.Create();
    }

    private static void CreateLocatedIn(Database db) => CreateEdge(db, "locatedIn");

    private static void CreateLivesIn(Database db) => CreateEdge(db, "livesIn");

    private static void CreateFriendOf(Database db) => CreateEdge(db, "friendOf");

    private static void CreateLikes(Database db) => CreateEdge(db, "likes", "rating");

    private static void CreateNode(Database db, string tableName, params string[] columns)
    {
        //
        //Drop the table if exists
        //
        const string schemaName = "dbo";

        ConsoleEx.WriteLine("Droping the table if exists", ConsoleColor.Red);
        if (db.Tables.Contains(tableName, schemaName))
            db.Tables[tableName, schemaName].Drop();
        ConsoleEx.WriteLine($"Create the table object {schemaName}.{tableName}", ConsoleColor.Cyan);

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
        tbl.Indexes.Add(idx);

        // Add the varchar columns
        foreach (var column in columns)
            tbl.Columns.Add(new Column(tbl, column, DataType.VarChar(100)));
        tbl.Create();
    }

    private static void CreateTableCity(Database db) => CreateNode(db, "City", "name", "stateName");

    private static void CreateTableRestaurant(Database db) => CreateNode(db, "Restaurant", "name", "city");

    private static void CreateTablePerson(Database db) => CreateNode(db, "Person", "name");
}
