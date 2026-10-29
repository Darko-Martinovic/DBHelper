using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

public sealed class CreateStoredProc : IExample
{
    public const string SchemaName = "dbo";
    public const string StoredProcedureName = "SmoTest";

    public string Description => "Shows how to create the stored procedure";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();
        var databaseName = context.DatabaseName;

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");
            var database = server.Databases[databaseName];

            if (database.StoredProcedures.Contains(StoredProcedureName, SchemaName))
            {
                // Removes the stored procedure from the instance of SQL Server.
                // To drop a stored procedure, users must have CONTROL permission on the stored procedure or be a member of the db_owner fixed database role.
                database.StoredProcedures[StoredProcedureName, SchemaName].Drop();
            }

            // To create a stored procedure, users must have CREATE PROCEDURE permission on the parent database
            // or be a member of the db_owner fixed database role.
            var newsp = new StoredProcedure(database, StoredProcedureName, SchemaName)
            {
                // TextMode the text header is not editable
                TextMode = false,

                // TextBody sets the Transact-SQL string that defines the stored procedure
                TextBody = "SELECT * FROM sys.tables WHERE name = @param",

                //  the stored procedure is encrypted
                IsEncrypted = true
            };
            // Parameter
            var parm = new StoredProcedureParameter(newsp, "@param") { DataType = DataType.SysName };
            newsp.Parameters.Add(parm);

            newsp.Create();
            Console.WriteLine("Successfully created the stored procedure ");

            //Script the stored procedure
            ConsoleEx.WriteScript(newsp.Script().Cast<string>());
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
