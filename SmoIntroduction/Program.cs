using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SmoIntroduction
{
    class Program
    {

        private const string C_DATABASENAME = "AdventureWorks2014";
        private const string C_NEWLINE = "\r\n";

        static void Main(string[] args)
        {

            StringBuilder sb = new StringBuilder();
            // Connect to the default instance
            // Be sure you have 'AdventureWorks2014' on default instance
            // or specify server on which exists 'AdventureWorks2014' database
            // ServerConnection cnn2 = new ServerConnection("<server name>");
            ServerConnection cnn = new ServerConnection();

            cnn.Connect();

            Console.Write("Connected" + C_NEWLINE);

            //Create the server object
            Server server = new Server(cnn);
            Console.Write("Create the server object - default instance" + C_NEWLINE);

           
            //Create the database object
            Database db = server.Databases[C_DATABASENAME];
            Console.Write("Create the database object - AdventureWorks2014" + C_NEWLINE);


            //----------------------------------------------------------------------
            //Get the server configuration
            //----------------------------------------------------------------------
            Configuration pc = server.Configuration;
            Type pcType = pc.GetType();
            sb.Append("----------------SERVER CONFIGURATION : " + server.Name + "----------------------------" + C_NEWLINE);
            foreach (ConfigProperty cp in pc.Properties)
            {
                sb.Append("\t" + cp.Description + C_NEWLINE);
                sb.Append("\t\t" + cp.DisplayName + " : " + cp.RunValue + C_NEWLINE);
            }

            //string fileName = server.Name.Replace(@"\", @"_") + ".txt";
            string fileName = "ServerConfig" + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".txt";



            if (File.Exists(fileName))
                File.Delete(fileName);

            File.WriteAllText(fileName, sb.ToString());
            // start notepad and disply the configuration
            Process.Start(fileName);

            if (cnn.IsOpen)
            {
                cnn.Disconnect();
                cnn = null;
            }

            if (server != null)
            {
                server = null;
            }
            Console.Write("Press any key to exit..." + C_NEWLINE);
            Console.ReadLine();
        }


    }
}

