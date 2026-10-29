using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

public sealed class ListAllUserDefinedDataTypes : IExample
{
    public string Description => "Shows how to get and recreate all user-defined data type in a database";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();
        var databaseName = context.DatabaseName;

        cnn.Connect();
        ConsoleEx.WriteLine("Connected", ConsoleColor.Blue);
        try
        {
            //Create the server object
            var server = new Server(cnn);
            ConsoleEx.WriteLine("Create the server object", ConsoleColor.Cyan);
            //Create the database object
            var db = server.Databases[databaseName];

            ConsoleEx.WriteLine($"List of all user defined data type in the database {db.Name}", ConsoleColor.Cyan);
            ConsoleEx.WriteLine("--------------------------------------------------------------------------", ConsoleColor.Yellow);

            // get all user defined data types
            var script = new List<string>();
            foreach (UserDefinedDataType t in db.UserDefinedDataTypes)
            {
                ConsoleEx.WriteLine($"{t.Schema}.{t.Name}", ConsoleColor.Red);
                script.AddRange(t.Script(MakeOptions()).Cast<string>());
                script.Add("");
            }

            ConsoleEx.WriteScript(script);
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

            ExtendedProperties = true,
            EnforceScriptingOptions = true,

            IncludeIfNotExists = true,
            IncludeHeaders = true,
            IncludeDatabaseContext = false,

            NoCommandTerminator = false,
            Permissions = true,
            SchemaQualify = true,
            SchemaQualifyForeignKeysReferences = true,

            NoCollation = false,
            NoExecuteAs = true,

            ScriptBatchTerminator = true,

            WithDependencies = true
        };
}
