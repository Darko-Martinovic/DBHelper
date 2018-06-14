using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction
{
    class ServerConfiguration
    {
        public static void Main(string[] args)
        {

            var sb = new StringBuilder();

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;

            ServerConnection cnn;


            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
            }


            cnn.Connect();

            Console.WriteLine("Connected");
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");


            //----------------------------------------------------------------------
            //Get the server configuration
            //----------------------------------------------------------------------
            var pc = server.Configuration;
            sb.AppendLine($"----------------SERVER CONFIGURATION : {server.Name}----------------------------");
            foreach (ConfigProperty cp in pc.Properties)
            {
                sb.AppendLine($"\t{cp.Description}");
                sb.AppendLine($"\t\t{cp.DisplayName} : {cp.RunValue}");
                Console.WriteLine($"\t{cp.Description}");
                Console.WriteLine($"\t\t{cp.DisplayName} : {cp.RunValue}");
            }


            var fileName = $"ServerConfig{DateTime.Now:yyyy_mm_dd_HH_mm_ss}.txt";



            if (File.Exists(fileName))
                File.Delete(fileName);

            File.WriteAllText(fileName, sb.ToString());
            // start notepad and disply the configuration
            Process.Start(fileName);

            if (cnn.IsOpen)
                cnn.Disconnect();
            cnn = null;
            server = null;
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
