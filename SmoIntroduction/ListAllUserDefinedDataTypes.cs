using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SmoIntroduction
{


    internal static class ListAllUserDefinedDataTypes
    {

        static void Main()
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
            ConsoleEx.WriteLine("Connected", ConsoleColor.Blue);
            //Create the server object
            var server = new Server(cnn);
            ConsoleEx.WriteLine("Create the server object", ConsoleColor.Cyan);
            //Create the database object
            var db = server.Databases[databaseName];

            ConsoleEx.WriteLine($"List of all user defined data type in the database {db.Name}", ConsoleColor.Cyan);
            ConsoleEx.WriteLine("--------------------------------------------------------------------------", ConsoleColor.Yellow);


            // get all user defined data types 
            var userDt = db.UserDefinedDataTypes;

            var result = new StringBuilder();
            foreach (UserDefinedDataType t in userDt)
            {
                ConsoleEx.WriteLine($"{t.Schema}{'.'}{t.Name}", ConsoleColor.Red);
                var helper = t.Script(MakeOptions());
                foreach (var h in helper)
                {
                    result.AppendLine(h);    
                }
                result.AppendLine();
            }

            var fileName = $"{db.Name}_UDDT_{DateTime.Now:yyyy_mm_dd_HH_mm_ss}.txt";
            if (File.Exists(fileName))
                File.Delete(fileName);
            File.WriteAllText(fileName, result.ToString());
            // start notepad and disply the configuration
            Process.Start(fileName);


            if (cnn.IsOpen)
                cnn.Disconnect();
            cnn = null;
            db = null;
            server = null;

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }



        public static ScriptingOptions MakeOptions()
        {
            ScriptingOptions o = null;
            try
            {
                o = new ScriptingOptions
                {
                    AllowSystemObjects = false,
                    AnsiFile = true,
                    AppendToFile = false,
                    AnsiPadding = true,




                    ExtendedProperties = true,
                    EnforceScriptingOptions = true,

                    IncludeIfNotExists = true,
                    IncludeHeaders = true,
                    IncludeDatabaseContext = false,



                    NoCommandTerminator = false,
                    Permissions = true,
                    SchemaQualify = true,
                    SchemaQualifyForeignKeysReferences = true,

                    NoCollation = false,
                    NoExecuteAs = true,
                    

                    ScriptBatchTerminator = true,


                    WithDependencies = true
                };



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            return o;
        }


    }
}
