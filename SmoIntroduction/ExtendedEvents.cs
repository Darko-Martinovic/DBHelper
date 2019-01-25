using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;


namespace SmoIntroduction
{
    // based on Microsoft's https://docs.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server?view=sql-server-2017

    static class ExtendedEventsDemo
    {


        static void Main()
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;

            var c = new SqlStoreConnection(new SqlConnection(connectionString));


            Console.WriteLine("Crreate the Session object");
            Console.WriteLine("The most important properties are : the session name and the parent");

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

            Console.WriteLine("Create the target. Be sure that C:\\TMP exists on your computer!");

            var t = new Target(x, "package0.event_file");

            //--== Specify target fields 

            var tf = t.TargetFields["fileName"];
            tf.Value = @"C:\Tmp\YourSession_Target.xel";
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
            Console.WriteLine("Press any key to exit ...");
            Console.ReadLine();




        }
    }
}
