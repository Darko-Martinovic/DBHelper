using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

public sealed class SetDefaultInitField : IExample
{
    public string Description => "Shows how to reduce the amount of T-SQL needed for object initialisation";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");

            // Demo, what are benefits of using 'SetDefaultInitFields' in the context of the server object.
            // In the example only four properties are used in the context of the database object. So, 'SetDefaultInitFields' should be used.
            // Benefit of using 'SetDefaultInitFields' is to reduce the amount of T-SQL needed for object initialisation
            // Besides properties names, we have to pass the database engine type.'Unknown' is allowed.

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
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }
}
