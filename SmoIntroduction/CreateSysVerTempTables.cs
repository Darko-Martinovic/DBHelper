using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoIntroduction
{
    public class CreateSysVerTempTables
    {
        private const string CServerVersion = "13.0.4001.0"; // https://support.microsoft.com/en-us/help/3182545

        static void Main(string[] args)
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            ServerConnection cnn;
            string databaseName;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
                // Read the database name from app.config
                databaseName = sqlConnection.Database;
            }


            cnn.Connect();
            Console.WriteLine("Connected");
            //Create the server object
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");
            //Create the database object

            if (server.Version <= new Version(CServerVersion))
            {
                ConsoleEx.WriteLine("Only supported for SQL 2016+");
                Console.ReadLine();
            }
            try
            {

                var db = server.Databases[databaseName];

            }
            catch
            {

            }

        }
    }
}
