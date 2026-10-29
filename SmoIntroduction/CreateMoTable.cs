using DBHelper;
using Microsoft.SqlServer.Management.Smo;
using Index = Microsoft.SqlServer.Management.Smo.Index;

namespace SmoIntroduction;

public sealed class CreateMoTable : IExample
{
    //Hard coded the file group name and the container name
    private const string CFileGroup = "mofg";
    private const string CFileName = "mofile";

    private const string CServerVersion = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545

    public string Description => "Shows how to create the memory optimized tables";

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
            // Only for SQL Server version 2016 SP1
            // Add MEMORY OPTIMIZED FILE GROUP AND FILE
            if (server.Version >= new Version(CServerVersion))
            {
                ConsoleEx.WriteLine("Add support for memory optimized tables", ConsoleColor.Cyan);
                // First check if there is already memory optimized file group
                var isMemoryOptimizedFileGroupExists = db.FileGroups.Cast<FileGroup>()
                    .Any(f => f.FileGroupType == FileGroupType.MemoryOptimizedDataFileGroup);
                if (isMemoryOptimizedFileGroupExists == false)
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
                        // The container is a folder on the server: MoContainerPath in the settings,
                        // or else a folder next to master's files
                        var prefix = context.Settings.MoContainerPath.Length > 0
                            ? context.Settings.MoContainerPath
                            : ServerPaths.Combine(server, server.Information.MasterDBPath, "MOCONTAINER");
                        var path = prefix + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
                        // Create the file ( the container )
                        var df = new DataFile(db.FileGroups[CFileGroup], CFileName, path);
                        // Add the container to the memory optimized file group
                        db.FileGroups[CFileGroup].Files.Add(df);
                        // Actually create. Now it exists in the database
                        db.FileGroups[CFileGroup].Files[CFileName].Create();
                    }
                }
            }
            //
            // end database operation - adding memory optimized file group
            //

            //
            //Create the schema if not exists
            //
            var schemaName = context.Settings.MoTestSchema;
            if (db.Schemas.Contains(schemaName) == false)
            {
                var hr = new Schema(db, schemaName);
                db.Schemas.Add(hr);
                db.Schemas[schemaName].Create();
            }

            ConsoleEx.WriteLine("Create the schema object - if not exists", ConsoleColor.Cyan);

            //
            //Drop the table if exists
            //
            var tableName = context.Settings.MoTestTable;
            if (db.Tables.Contains(tableName, schemaName))
                db.Tables[tableName, schemaName].Drop();
            ConsoleEx.WriteLine("Droping the table if exists", ConsoleColor.Red);

            ConsoleEx.WriteLine($"Create the table object {schemaName}.{tableName}", ConsoleColor.Cyan);

            //
            // Create a new table object
            //
            var tbl = new Table(db, tableName, schemaName)
            {
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

            tbl.Indexes.Add(idx);
            idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));

            // Add the varchar column
            col = new Column(tbl, @"Name", DataType.VarChar(128));
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

            ConsoleEx.WriteLine("Adding the table columns", ConsoleColor.DarkGray);
            // Create the table
            tbl.Create();

            ConsoleEx.WriteLine($"Create the table on SQL Server {schemaName}.{tableName}", ConsoleColor.Cyan);

            ConsoleEx.WriteLine($"Make T-SQL script to create table {schemaName}.{tableName}", ConsoleColor.Yellow);
            ConsoleEx.WriteScript(tbl.Script(CreateTable.MakeOptions()).Cast<string>());
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
