using Microsoft.SqlServer.Management.Smo;
using Index = Microsoft.SqlServer.Management.Smo.Index;

namespace SmoIntroduction;

public sealed class CreateTable : IExample
{
    public string Description => "Shows how to create traditional - disk based tables";

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

            var schemaName = context.Settings.TestSchema;
            var tableName = context.Settings.TestTable;
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
            tbl.Indexes.Add(idx);

            // Add the varchar column
            col = new Column(tbl, @"Name", DataType.VarChar(128));
            col.AddDefaultConstraint(null);
            col.DefaultConstraint.Text = "''";
            col.Nullable = false;
            tbl.Columns.Add(col);

            // Add the datetime column
            col = new Column(tbl, @"Date", DataType.DateTime);
            tbl.Columns.Add(col);
            col.Nullable = false;

            Console.WriteLine("Adding the table columns");
            // Create the table
            tbl.Create();

            Console.WriteLine($"Create the table on SQL Server {schemaName}.{tableName}");

            Console.WriteLine($"Make T-SQL script to create table {schemaName}.{tableName}");
            ConsoleEx.WriteScript(tbl.Script(MakeOptions()).Cast<string>());
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }

    public static ScriptingOptions MakeOptions() =>
        new()
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
