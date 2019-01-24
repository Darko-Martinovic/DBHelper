using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;


namespace SmoIntroduction
{
    // based on Microsoft's https://docs.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server?view=sql-server-2017

    class ExtendedEventsDemo
    {

        private static Server _server;

        private static string _dbName = "ExtendedEventsDemo";

        static void Main()
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            ServerConnection cnn;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);
            }

            cnn.Connect();

            Console.WriteLine("Connected");
            _server = new Server(cnn);
            Console.WriteLine("Create the server object");
            // Drop the database if exists
            if (_server.Databases[_dbName] != null)
                _server.Databases[_dbName].Drop();

            //Create the database
            var db = new Database(_server, _dbName);
            db.Create();

            //
            var x = new Session
            {
                MaxMemory = 2048,
                EventRetentionMode = Session.EventRetentionModeEnum.AllowMultipleEventLoss,
                MaxDispatchLatency =  3,
                MaxEventSize = 0,
                MemoryPartitionMode = Session.MemoryPartitionModeEnum.None,
                TrackCausality = false,
                AutoStart = false
            };
            var t = new Target();
            t.Parent = x;

            //var ti = new TargetInfo();
            

            x.Targets.Add(t);

           var e = new Event();
           x.Events.Add(e);

            x.Create();
            x.Start();


            


        }
    }
}
