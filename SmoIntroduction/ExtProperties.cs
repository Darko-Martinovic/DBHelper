using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction;

/// <summary>Sets an extended property on objects of many kinds in AdventureWorks.</summary>
public sealed class ExtProperties : IExample
{
    public const string Creator = "Creator";
    public const string Value = "Simple Talk";

    public string Description => "Shows how to manipulate with extended properties";

    public void Run(ExampleContext context)
    {
        var cnn = context.Connect();
        var databaseName = context.DatabaseName;

        cnn.Connect();
        Console.WriteLine("Connected");
        try
        {
            //Create the server object
            var server = new Server(cnn);
            Console.WriteLine("Create the server object");

            //Create the database object
            var db = server.Databases[databaseName];
            Console.WriteLine($"Create the database object - {databaseName}");

            //Setup the extended property on database level
            SetCreator(db, db.ExtendedProperties, "database");

            //----------------------------------------------------------------------
            // Setup the extended property on schema level
            // db.Schemas is the way how we access schemas collection
            //----------------------------------------------------------------------
            var sch = db.Schemas["HumanResources"];
            SetCreator(sch, sch.ExtendedProperties, "schema");

            //----------------------------------------------------------------------
            // Setup the extended property on table level
            // db.Tables is the way how we access tables collection
            //----------------------------------------------------------------------
            var tbl = db.Tables["Employee", "HumanResources"];
            SetCreator(tbl, tbl.ExtendedProperties, "table");

            //----------------------------------------------------------------------
            // Setup the extended property on column level
            // tbl.Columns is the way how we access columns collection
            //----------------------------------------------------------------------
            var column = tbl.Columns["NationalIDNumber"];
            SetCreator(column, column.ExtendedProperties, "column");

            //----------------------------------------------------------------------
            // Setup the extended property on index level
            // tbl.Indexes is the way how we access indexes collection
            //----------------------------------------------------------------------
            var index = tbl.Indexes["PK_Employee_BusinessEntityID"];
            SetCreator(index, index.ExtendedProperties, "index");

            //----------------------------------------------------------------------
            // Setup the extended property on storedProcedure level
            // db.StoredProcedures is the way how we access StoredProcedures collection
            //----------------------------------------------------------------------
            var sp = db.StoredProcedures["uspUpdateEmployeeHireInfo", "HumanResources"];
            SetCreator(sp, sp.ExtendedProperties, "stored procedure");

            //----------------------------------------------------------------------
            // Setup the extended property on constraint level
            // tbl.Checks is the way how we access checks collection
            //----------------------------------------------------------------------
            var check = tbl.Checks["CK_Employee_BirthDate"];
            SetCreator(check, check.ExtendedProperties, "constraint");

            //----------------------------------------------------------------------
            // Setup the extended property on view level
            // db.Views is the way how we access views collection
            //----------------------------------------------------------------------
            var view = db.Views["vEmployee", "HumanResources"];
            SetCreator(view, view.ExtendedProperties, "view");

            //----------------------------------------------------------------------
            // Setup the extended property on XmlSchemaCollection level
            // db.XmlSchemaCollections is the way how we access XmlSchemaCollections collection
            //----------------------------------------------------------------------
            var xmlsc = db.XmlSchemaCollections["IndividualSurveySchemaCollection", "Person"];
            SetCreator(xmlsc, xmlsc.ExtendedProperties, "XML schema collection");

            //----------------------------------------------------------------------
            // Setup the extended property on ForeignKey level
            // tbl.ForeignKeys is the way how we access ForeignKeys collection
            //----------------------------------------------------------------------
            var fk = tbl.ForeignKeys["FK_Employee_Person_BusinessEntityID"];
            SetCreator(fk, fk.ExtendedProperties, "foreign key");

            //----------------------------------------------------------------------
            // Setup the extended property on UserDefinedDataType level
            // db.UserDefinedDataTypes is the way how we access UserDefinedDataTypes collection
            //----------------------------------------------------------------------
            var types2 = db.UserDefinedDataTypes["Flag"];
            SetCreator(types2, types2.ExtendedProperties, "user-defined type");
        }
        finally
        {
            if (cnn.IsOpen)
                cnn.Disconnect();
        }
    }

    // Creates the property, or changes its value when it is already there
    private static void SetCreator(SqlSmoObject parent, ExtendedPropertyCollection properties, string level)
    {
        var extProperty = properties[Creator];
        if (extProperty == null)
        {
            extProperty = new ExtendedProperty
            {
                Parent = parent,
                Name = Creator,
                Value = Value
            };
            extProperty.Create();
        }
        else
        {
            extProperty.Value = Value;
            extProperty.Alter();
        }

        Console.WriteLine($"Setup the extended property on {level} level");
    }
}
