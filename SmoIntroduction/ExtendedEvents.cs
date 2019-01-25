using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.XEventDbScoped;


namespace SmoIntroduction
{
    // based on Microsoft's https://docs.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server?view=sql-server-2017

    static class ExtendedEventsDemo
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
            var c = new SqlStoreConnection(new SqlConnection(connectionString));
            var x = new Session()
            {
                
                Parent = new XEStore(c),
                MaxMemory = 2048,
                EventRetentionMode = Session.EventRetentionModeEnum.AllowMultipleEventLoss,
                MaxDispatchLatency =  3,
                MaxEventSize = 0,
                MemoryPartitionMode = Session.MemoryPartitionModeEnum.None,
                TrackCausality = false,
                AutoStart = false,
                Name =  "SmoSession"
            };

            var t = new Target(x, "package0.event_file");
            var tf = t.TargetFields["fileName"];
            tf.Value = @"C:\Tmp\YourSession_Target.xel";
            tf = t.TargetFields["max_file_size"];
            tf.Value = 2;
            tf = t.TargetFields["max_rollover_files"];
            tf.Value = 2;


            x.Targets.Add(t);

            var e = new Event(x, "sqlserver.sql_statement_completed");

            //var ef = new EventField {Parent = e, Name = "[sqlserver].[like_i_sql_unicode_string]([sqlserver].[sql_text]", Value = "%SELECT%HAVING%"};
            //e.EventFields.Add(ef);

            var a = new Microsoft.SqlServer.Management.XEvent.Action(e, "sqlserver.sql_text");




            e.Actions.Add(a);
            x.Events.Add(e);

            x.Create();
            x.Start();


            


        }
    }
}
