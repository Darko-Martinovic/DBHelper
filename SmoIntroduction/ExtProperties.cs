using System;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction
{

    public class ExtProperties
    {


        private const string CDatabasename = "AdventureWorks2014";
        private const string Createor = "Creator";
        private const string Value = "Simple Talk";
       

        public static void Main(string[] args)
        {
            var sb = new StringBuilder();
           
            // Connect to the default instance
            // Be sure you have 'AdventureWorks2014' on default instance
            // or specify server on which exists 'AdventureWorks2014' database
            // ServerConnection cnn2 = new ServerConnection("<server name>");
            var cnn = new ServerConnection();

            cnn.Connect();


           

            Console.WriteLine("Connected");

            //Create the server object
            var server = new Server(cnn);
            Console.WriteLine("Create the server object - default instance");


            //Create the database object
            var db = server.Databases[CDatabasename];
            Console.WriteLine("Create the database object - AdventureWorks2014");


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
            Console.WriteLine("Setup the extended property");

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
            Console.WriteLine("Setup the extended property on table level");

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
            Console.WriteLine("Setup the extended property on column level");

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
            Console.WriteLine("Setup the extended property on index level");


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
            Console.WriteLine("Setup the extended property on stored procedure level");


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
            Console.WriteLine("Setup the extended property on constraint level");

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
            Console.WriteLine("Setup the extended property on view level");

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
            Console.WriteLine("Setup the extended property on XML schema collection level");

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

            Console.WriteLine("Setup the extended property on foreign key level");

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
            Console.WriteLine("Setup the extended property on user-defined type level");

            if (cnn.IsOpen)
            {
                cnn.Disconnect();
                cnn = null;
            }


           
            server = null;
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

        }
    }
}
