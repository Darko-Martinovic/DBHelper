using DBHelper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

/// <summary>
/// SMO receives server events through WMI, so this example needs Windows on both sides:
/// the client and a SQL Server on Windows, whose WMI the client can reach.
/// </summary>
public sealed class ServerEvents : IExample
{
    public string Description => "Shows how to subscribe to server events (Windows only)";

    public void Run(ExampleContext context)
    {
        if (!OperatingSystem.IsWindows())
        {
            ConsoleEx.WriteLine("SMO receives server events through WMI, which only Windows has.", ConsoleColor.Yellow);
            return;
        }

        var cnn = context.Connect();

        cnn.Connect();
        Console.WriteLine("Connected");
        var server = new Server(cnn);
        Console.WriteLine("Create the server object");
        try
        {
            if (server.HostPlatform != HostPlatformNames.Windows)
            {
                ConsoleEx.WriteLine($"SMO receives server events through WMI, and {server.Name} runs on {server.HostPlatform}.", ConsoleColor.Yellow);
                return;
            }

            server.Events.ServerEvent += Events_ServerEvent;

            // ServerEvent has a property for each event there is: CreateTable, DropTable, AlterLogin...
            // Subscribe to create & drop table events
            server.Events.SubscribeToEvents(ServerEvent.CreateTable + ServerEvent.DropTable);

            Console.WriteLine(new string('-', 79));
            Console.WriteLine($@"Starting events for server: {server.Name}");
            Console.WriteLine(new string('-', 79));
            Console.WriteLine($@"Capturing events {ServerEvent.CreateTable} and {ServerEvent.DropTable}");
            Console.WriteLine(new string('-', 79));
            Console.WriteLine(@"Try creating and droping tables (press any key to stop)");
            Console.WriteLine(new string('-', 79));

            // Start receiving events
            server.Events.StartEvents();

            // wait for events
            if (Console.IsInputRedirected)
                Console.ReadLine();
            else
                Console.ReadKey(intercept: true);
        }
        finally
        {
            Console.WriteLine("Exiting - Unsubscribe from all the events");
            server.Events.UnsubscribeAllEvents();
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }

    private static void Events_ServerEvent(object sender, ServerEventArgs e)
    {
        try
        {
            Console.WriteLine(
                @"EventType: {0, -20} SPID: {1, 4} PostTime: {2, -20}",
                e.EventType, e.Spid, e.PostTime);

            foreach (EventProperty ep in e.Properties)
            {
                if (ep.Value != null)
                {
                    Console.WriteLine("\t{0, -30} {1, -30}", ep.Name, ep.Value);
                }
                else
                    Console.WriteLine("\t{0,-30}", ep.Name);
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', 79));
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.AllMessages());
        }
    }
}
