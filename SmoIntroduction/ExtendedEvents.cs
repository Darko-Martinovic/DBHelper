using DBHelper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;

namespace SmoIntroduction;

// based on Microsoft's https://learn.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server
public sealed class ExtendedEvents : IExample
{
    public const string SessionName = "SmoSession";

    public string Description => "Shows how to manipulate with extended events ( create session, add targets, add events )";

    public void Run(ExampleContext context)
    {
        // The target file goes next to the server's error log, where the server can write
        var cnn = context.Connect();
        string fileName;
        try
        {
            var server = new Server(cnn);
            fileName = ServerPaths.Combine(server, server.ErrorLogPath, $"{SessionName}_Target.xel");
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }

        var c = new SqlStoreConnection(new SqlConnection(context.ConnectionString));

        Console.WriteLine("Create the Session object");
        Console.WriteLine("The most important properties are : the session name and the parent");
        var se = new XEStore(c);
        se.Sessions[SessionName]?.Drop();

        var x = new Session
        {
            Parent = se,
            MaxMemory = 2048,
            EventRetentionMode = Session.EventRetentionModeEnum.AllowMultipleEventLoss,
            MaxDispatchLatency = 3,
            MaxEventSize = 0,
            MemoryPartitionMode = Session.MemoryPartitionModeEnum.None,
            TrackCausality = false,
            AutoStart = false,
            Name = SessionName
        };

        Console.WriteLine($"Create the target, the file {fileName} on the server");

        var t = new Target(x, "package0.event_file");

        //--== Specify target fields

        var tf = t.TargetFields["filename"];
        tf.Value = fileName;
        tf = t.TargetFields["max_file_size"];
        tf.Value = 2;
        tf = t.TargetFields["max_rollover_files"];
        tf.Value = 2;
        x.Targets.Add(t);

        Console.WriteLine("Create the event");
        var e = new Event(x, "sqlserver.sql_statement_completed");

        Console.WriteLine("Create the Action");
        var a = new Microsoft.SqlServer.Management.XEvent.Action(e, "sqlserver.sql_text");

        Console.WriteLine("Predicate expression");
        e.PredicateExpression = "( [sqlserver].[like_i_sql_unicode_string]([sqlserver].[sql_text], N'%SELECT%HAVING%') )";

        e.Actions.Add(a);
        x.Events.Add(e);

        Console.WriteLine("Create the Session");
        x.Create();
        Console.WriteLine("Start the Session");
        x.Start();
        ConsoleEx.WriteLine($"The session {SessionName} is running. Run the example again to recreate it, or drop it with DROP EVENT SESSION [{SessionName}] ON SERVER.",
            ConsoleColor.Cyan);
    }
}
