using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using static System.Console;

namespace SmoIntroduction
{
    class Program
    {

        private const string CDatabasename = "AdventureWorks2014";
        private const string Createor = "Creator";
        private const string Value = "Simple Talk";
        private const string CNewline = "\r\n";

        public static void Main(string[] args)
        {

            var sb = new StringBuilder();
            // Connect to the default instance
            // Be sure you have 'AdventureWorks2014' on default instance
            // or specify server on which exists 'AdventureWorks2014' database
            // ServerConnection cnn2 = new ServerConnection("<server name>");
            var cnn = new ServerConnection();

            cnn.Connect();

            Write("Connected" + CNewline);

            //Create the server object
            var server = new Server(cnn);
            Write($"Create the server object - default instance{CNewline}");


            //Create the database object
            var db = server.Databases[CDatabasename];
            Write($"Create the database object - AdventureWorks2014{CNewline}");


            //Setup the extended property on database level
            ExtendedProperty extProperty;
            if (db.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = db,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = db.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on schema level
            // db.Schemas is the way how we access schemas collection
            //----------------------------------------------------------------------
            var sch = db.Schemas["HumanResources"];
            if (sch.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = sch,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = sch.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }


            //----------------------------------------------------------------------
            // Setup the extended property on table level
            // db.Tables is the way how we access tables collection
            //----------------------------------------------------------------------
            var tbl = db.Tables["Employee", "HumanResources"];
            if (tbl.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = tbl,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = tbl.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on table level{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on column level
            // tbl.Columns is the way how we access columns collection
            //----------------------------------------------------------------------
            if (tbl.Columns["NationalIDNumber"].ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = tbl.Columns["NationalIDNumber"],
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = tbl.Columns["NationalIDNumber"].ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on column level{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on index level
            // tbl.Indexes is the way how we access indexes collection
            //----------------------------------------------------------------------
            if (tbl.Indexes["PK_Employee_BusinessEntityID"].ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = tbl.Indexes["PK_Employee_BusinessEntityID"],
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = tbl.Indexes["PK_Employee_BusinessEntityID"].ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on index level{CNewline}");


            //----------------------------------------------------------------------
            // Setup the extended property on storedProcedure level
            // tbl.StoredProcedures is the way how we access StoredProcedures collection
            //----------------------------------------------------------------------
            var sp = db.StoredProcedures["uspUpdateEmployeeHireInfo", "HumanResources"];
            if (sp.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = sp,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = sp.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on stored procedure level{CNewline}");


            //----------------------------------------------------------------------
            // Setup the extended property on constraint level
            // tbl.Checks is the way how we access checks collection
            //----------------------------------------------------------------------
            if (tbl.Checks["CK_Employee_BirthDate"].ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = tbl.Checks["CK_Employee_BirthDate"],
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = tbl.Checks["CK_Employee_BirthDate"].ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on constraint level{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on view level
            // db.Views is the way how we access views collection
            //----------------------------------------------------------------------
            if (db.Views["vEmployee", "HumanResources"].ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = db.Views["vEmployee", "HumanResources"],
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = db.Views["vEmployee", "HumanResources"].ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on view level{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on XmlSchemaCollection level
            // db.XmlSchemaCollections is the way how we access XmlSchemaCollections collection
            //----------------------------------------------------------------------
            var xmlsc = db.XmlSchemaCollections["IndividualSurveySchemaCollection", "Person"];
            if (xmlsc.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = xmlsc,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = xmlsc.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on XML schema collection level{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on ForeignKey level
            // tbl.ForeignKeys is the way how we access ForeignKeys collection
            //----------------------------------------------------------------------
            var fk = tbl.ForeignKeys["FK_Employee_Person_BusinessEntityID"];
            if (fk.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = fk,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = fk.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }

            Write($"Setup the extended property on foreign key level{CNewline}");

            //----------------------------------------------------------------------
            // Setup the extended property on UserDefinedDataType level
            // db.UserDefinedDataType is the way how we access UserDefinedDataTypes collection
            //----------------------------------------------------------------------
            var types2 = db.UserDefinedDataTypes["Flag"];
            if (types2.ExtendedProperties[Createor] == null)
            {
                extProperty = new ExtendedProperty
                {
                    Parent = types2,
                    Name = Createor,
                    Value = Value
                };
                extProperty.Create();
            }
            else
            {
                extProperty = types2.ExtendedProperties[Createor];
                extProperty.Value = Value;
                extProperty.Alter();
            }
            Write($"Setup the extended property on user-defined type level{CNewline}");



            //----------------------------------------------------------------------
            //Get the server configuration
            //----------------------------------------------------------------------
            var pc = server.Configuration;
            sb.Append($"----------------SERVER CONFIGURATION : {server.Name}----------------------------{CNewline}");
            foreach (ConfigProperty cp in pc.Properties)
            {
                sb.Append($"\t{cp.Description}{CNewline}");
                sb.Append($"\t\t{cp.DisplayName} : {cp.RunValue}{CNewline}");
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
            Write($"Press any key to exit...{CNewline}");
            ReadLine();
        }


    }
}

