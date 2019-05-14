using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction
{
    internal static class SetDefaultInitField
    {
        public static void Main()
        {

            var connectionString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;

            ServerConnection cnn;

            var ds = new DataSet();
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                cnn = new ServerConnection(sqlConnection);

                var custAdapter = new SqlDataAdapter(" SELECT * FROM DC.GetIntermediateResults;", sqlConnection);
                custAdapter.Fill(ds);
            }


            cnn.Connect();

            Console.WriteLine("Connected");
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");

            // Demo, what are benefits of using 'SetDefaultInitFields' in the context of the server object.
            // In the example only four properties are used in the context of the database object. So, 'SetDefaultInitFields' should be used.
            // Benefit of using 'SetDefaultInitFields' is to reduce the amount of T-SQL needed for object initialisation
            // Besides properties names, we have to pass the database engine type.'Unknowen' is allowed.

            server.SetDefaultInitFields(typeof(Database), DatabaseEngineEdition.Unknown, "Name", "AutoClose",
                "AutoShrink", "AutoCreateStatisticsEnabled");

            // In the context of the table object we might use following call
            //server.SetDefaultInitFields(typeof(Table), DatabaseEngineEdition.Unknown, "Name", "HasPrimaryClusteredIndex", "RowCount");

            // Limit the database properties returned to just those that we use
            // -----------------------------------------------------------------------
            // T-SQL will execute when 'server.Databases' is reached
            // SMO optimization allows only the minimum properties to be loaded when an object is created.
            // When uninitialized properties are accessed, SMO makes individual requests to load the information from the instance of SQL Server.
            // You can use this method to adjust which properties are initialized for an object when it is first created to further optimize performance.

            //SELECT
            //dtb.name AS[Name],
            //dtb.is_auto_close_on AS[AutoClose],
            //dtb.is_auto_shrink_on AS[AutoShrink],
            //dtb.is_auto_create_stats_on AS[AutoCreateStatisticsEnabled]
            //FROM
            //master.sys.databases AS dtb
            //ORDER BY
            //[Name] ASC




            ConsoleEx.WriteLine(
                "Database name                                 Is Auto Close Enabled    Is Auto Shrink Enabled     Is Auto Create Statistics Enabled",
                ConsoleColor.Yellow);
            ConsoleEx.WriteLine("------------------------------------------------------------------------------------------------------------------------");

            foreach (Database db in server.Databases)
            {

                Console.WriteLine(
                    "{0}                    {1}                         {2}                           {3}", db.Name.Trim().PadRight(25),
                    db.AutoClose, db.AutoShrink, db.AutoCreateStatisticsEnabled);

            }

            Console.ReadLine();
        }
    }
}
