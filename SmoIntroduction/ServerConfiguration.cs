using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

public sealed class ServerConfiguration : IExample
{
    public string Description => "Shows how to get the server configuration";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");

            //----------------------------------------------------------------------
            //Get the server configuration
            //----------------------------------------------------------------------
            var pc = server.Configuration;
            Console.WriteLine($"----------------SERVER CONFIGURATION : {server.Name}----------------------------");
            foreach (ConfigProperty cp in pc.Properties)
            {
                Console.WriteLine($"\t{cp.Description}");
                Console.WriteLine($"\t\t{cp.DisplayName} : {cp.RunValue}");
            }
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
