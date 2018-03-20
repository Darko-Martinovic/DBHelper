using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Text;

namespace SmoIntroduction
{
    class Program
    {

        private const string C_DATABASENAME = "AdventureWorks2014";
        private const string CREATEOR = "Creator";
        private const string VALUE = "Simple Talk";
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

            string bkpDirectory = server.BackupDirectory;


            //Create the database object
            Database db = server.Databases[C_DATABASENAME];
            Console.Write("Create the database object - AdventureWorks2014" + C_NEWLINE);


            //Setup the extended property on database level
            ExtendedProperty extProperty = null;
            if (db.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = db;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = db.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property" + C_NEWLINE);

           
            Console.Write("Press any key to exit..." + C_NEWLINE);
            Console.ReadLine();
        }


    }
}

