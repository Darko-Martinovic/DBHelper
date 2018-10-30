using Converter.Extension;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace SmoIntroduction
{
    internal static class CreateStoredProc
    {
        private const string SchemaName = "DBO";
        private const string StoredProcedureName = "SmoTest";

        private const string CTmpStoredprocedureSql = @"C:\TMP\StoredProcedure.SQL";

        public static void Main()
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
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");
            var database = server.Databases[databaseName];

            if (database.StoredProcedures.Contains(StoredProcedureName, SchemaName))
            {
                // Removes the stored procedure from the instance of SQL Server.
                // To drop a stored procedure, users must have CONTROL permission on the stored procedure or be a member of the db_owner fixed database role. 
                database.StoredProcedures[StoredProcedureName, SchemaName].Drop();
            }

            // To create a stored procedure, users must have CREATE PROCEDURE permission on the parent database
            // or be a member of the db_owner fixed database role. 
            var newsp = new StoredProcedure(database, StoredProcedureName, SchemaName)
            {
                // TextMode the text header is not editable
                TextMode = false,

                // TextBody sets the Transact-SQL string that defines the stored procedure
                TextBody = "SELECT * FROM sys.tables WHERE name = @param",

                //  the stored procedure is encrypted
                IsEncrypted = true

            };
            // Parameter 
            var parm = new StoredProcedureParameter(newsp, "@param") { DataType = DataType.SysName };
            newsp.Parameters.Add(parm);


            try
            {
                newsp.Create();
                Console.WriteLine("Successfully created the stored procedure ");

                //Script the stored procedure 
                var result = newsp.Script();

                // Write scripting output to sql file
                File.WriteAllLines(CTmpStoredprocedureSql, result.Cast<string>());

                // Start NOTEPAD and display T-SQL script
                Process.Start("notepad", CTmpStoredprocedureSql);


            }
            catch (Exception ex)
            {

                var error = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message));

                Console.WriteLine("An error occured while creating the stored procedure. The error message is : " + error);

            }
            Console.ReadLine();

        }
    }
}
