﻿using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace SmoIntroduction
{
    class Program
    {

        private const string C_DATABASENAME = "AdventureWorks2014";
        private const string CREATEOR = "Creator";
        private const string VALUE = "Simple Talk";
        private const string C_NEWLINE = "\r\n";

        static void Main(string[] args)
        {

            StringBuilder sb = new StringBuilder();
            // Connect to the default instance
            // Be sure you have 'AdventureWorks2014' on default instance
            // or specify server on which exists 'AdventureWorks2014' database
            // ServerConnection cnn2 = new ServerConnection("<server name>");
            ServerConnection cnn = new ServerConnection();

            cnn.Connect();

            Console.Write("Connected" + C_NEWLINE);

            //Create the server object
            Server server = new Server(cnn);
            Console.Write("Create the server object - default instance" + C_NEWLINE);

            string bkpDirectory = server.BackupDirectory;


            //Create the database object
            Database db = server.Databases[C_DATABASENAME];
            Console.Write("Create the database object - AdventureWorks2014" + C_NEWLINE);


            //Setup the extended property on database level
            ExtendedProperty extProperty = null;
            if (db.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = db;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = db.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property" + C_NEWLINE);

            Schema sch = db.Schemas["HumanResources"];
            if (sch.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = sch;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = sch.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }

            Table tbl = db.Tables["Employee", "HumanResources"];
            if (tbl.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = tbl;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = tbl.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on table level" + C_NEWLINE);

            Column column = tbl.Columns["NationalIDNumber"];
            if (column.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = column;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = column.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on column level" + C_NEWLINE);

            Index ind = tbl.Indexes["PK_Employee_BusinessEntityID"];
            if (ind.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = ind;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = ind.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on index level" + C_NEWLINE);

            StoredProcedure sp = db.StoredProcedures["uspUpdateEmployeeHireInfo", "HumanResources"];
            if (sp.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = sp;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = sp.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on stored procedure level" + C_NEWLINE);

            Check cons = tbl.Checks["CK_Employee_BirthDate"];
            if (cons.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = cons;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = cons.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on constraint level" + C_NEWLINE);

            View view = db.Views["vEmployee", "HumanResources"];
            if (view.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = view;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = view.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on view level" + C_NEWLINE);

            XmlSchemaCollection xmlsc = db.XmlSchemaCollections["IndividualSurveySchemaCollection", "Person"];
            if (xmlsc.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = xmlsc;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = xmlsc.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on XML schema collection level" + C_NEWLINE);

            ForeignKey fk = tbl.ForeignKeys["FK_Employee_Person_BusinessEntityID"];
            if (fk.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = fk;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = fk.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }

            Console.Write("Setup the extended property on foreign key level" + C_NEWLINE);

            UserDefinedDataType types2 = db.UserDefinedDataTypes["Flag"];
            if (types2.ExtendedProperties[CREATEOR] == null)
            {
                extProperty = new ExtendedProperty();
                extProperty.Parent = types2;
                extProperty.Name = CREATEOR;
                extProperty.Value = VALUE;
                extProperty.Create();
            }
            else
            {
                extProperty = types2.ExtendedProperties[CREATEOR];
                extProperty.Value = VALUE;
                extProperty.Alter();
            }
            Console.Write("Setup the extended property on user-defined type level" + C_NEWLINE);




            //Get the server configuration
            Configuration pc = server.Configuration;
            Type pcType = pc.GetType();
            sb.Append("----------------SERVER CONFIGURATION : " + server.Name + "----------------------------" + C_NEWLINE);
            foreach (ConfigProperty cp in pc.Properties)
            {
                sb.Append("\t" + cp.Description + C_NEWLINE);
                sb.Append("\t\t" + cp.DisplayName + " : " + cp.RunValue + C_NEWLINE);
            }
            string fileName = server.Name + ".txt";



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
            Console.Write("Press any key to exit..." + C_NEWLINE);
            Console.ReadLine();
        }

        public static class Misc
        {

            public static string DecryptSecureString(SecureString ss)
            {
                string str = string.Empty;
                if (ss != null)
                {
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                    IntPtr ptr = Marshal.SecureStringToBSTR(ss);
                    str = Marshal.PtrToStringBSTR(ptr);
                    Marshal.ZeroFreeBSTR(ptr);
                }
                return str;
            }

            public static SecureString EncryptString(string s)
            {
                SecureString str = new SecureString();
                if (s != null)
                {
                    foreach (char ch in s.ToCharArray())
                    {
                        str.AppendChar(ch);
                    }
                }
                return str;
            }
        }

    }
}

