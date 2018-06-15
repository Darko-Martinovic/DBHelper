using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using Converter.Extension;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SmoIntroduction
{
    class ServerEvents
    {
        private static Server _server;
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


            _server.Events.ServerEvent += Events_ServerEvent;
            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);



            // see complete events list below 
            //---------------------------------------------------------------------    
            //      internal enum ServerEventValues
            //{
            //    AddRoleMember,
            //    AddServerRoleMember,
            //    AddSignature,
            //    AddSignatureSchemaObject,
            //    AlterApplicationRole,
            //    AlterAssembly,
            //    AlterAsymmetricKey,
            //    AlterAudit,
            //    AlterAuthorizationDatabase,
            //    AlterAuthorizationServer,
            //    AlterAvailabilityGroup,
            //    AlterBrokerPriority,
            //    AlterCertificate,
            //    AlterColumnEncryptionKey,
            //    AlterCredential,
            //    AlterCryptographicProvider,
            //    AlterDatabase,
            //    AlterDatabaseAuditSpecification,
            //    AlterDatabaseEncryptionKey,
            //    AlterEndpoint,
            //    AlterEventSession,
            //    AlterExtendedProperty,
            //    AlterFulltextCatalog,
            //    AlterFulltextIndex,
            //    AlterFulltextStoplist,
            //    AlterFunction,
            //    AlterIndex,
            //    AlterInstance,
            //    AlterLinkedServer,
            //    AlterLogin,
            //    AlterMasterKey,
            //    AlterMessage,
            //    AlterMessageType,
            //    AlterPartitionFunction,
            //    AlterPartitionScheme,
            //    AlterPlanGuide,
            //    AlterProcedure,
            //    AlterQueue,
            //    AlterRemoteServer,
            //    AlterRemoteServiceBinding,
            //    AlterResourceGovernorConfig,
            //    AlterResourcePool,
            //    AlterRole,
            //    AlterRoute,
            //    AlterSchema,
            //    AlterSearchPropertyList,
            //    AlterSecurityPolicy,
            //    AlterSequence,
            //    AlterServerAudit,
            //    AlterServerAuditSpecification,
            //    AlterServerConfiguration,
            //    AlterServerRole,
            //    AlterService,
            //    AlterServiceMasterKey,
            //    AlterSymmetricKey,
            //    AlterTable,
            //    AlterTrigger,
            //    AlterUser,
            //    AlterView,
            //    AlterWorkloadGroup,
            //    AlterXmlSchemaCollection,
            //    BindDefault,
            //    BindRule,
            //    CreateApplicationRole,
            //    CreateAssembly,
            //    CreateAsymmetricKey,
            //    CreateAudit,
            //    CreateAvailabilityGroup,
            //    CreateBrokerPriority,
            //    CreateCertificate,
            //    CreateColumnEncryptionKey,
            //    CreateColumnMasterKey,
            //    CreateContract,
            //    CreateCredential,
            //    CreateCryptographicProvider,
            //    CreateDatabase,
            //    CreateDatabaseAuditSpecification,
            //    CreateDatabaseEncryptionKey,
            //    CreateDefault,
            //    CreateEndpoint,
            //    CreateEventNotification,
            //    CreateEventSession,
            //    CreateExtendedProcedure,
            //    CreateExtendedProperty,
            //    CreateFulltextCatalog,
            //    CreateFulltextIndex,
            //    CreateFulltextStoplist,
            //    CreateFunction,
            //    CreateIndex,
            //    CreateLinkedServer,
            //    CreateLinkedServerLogin,
            //    CreateLogin,
            //    CreateMasterKey,
            //    CreateMessage,
            //    CreateMessageType,
            //    CreatePartitionFunction,
            //    CreatePartitionScheme,
            //    CreatePlanGuide,
            //    CreateProcedure,
            //    CreateQueue,
            //    CreateRemoteServer,
            //    CreateRemoteServiceBinding,
            //    CreateResourcePool,
            //    CreateRole,
            //    CreateRoute,
            //    CreateRule,
            //    CreateSchema,
            //    CreateSearchPropertyList,
            //    CreateSecurityPolicy,
            //    CreateSequence,
            //    CreateServerAudit,
            //    CreateServerAuditSpecification,
            //    CreateServerRole,
            //    CreateService,
            //    CreateSpatialIndex,
            //    CreateStatistics,
            //    CreateSymmetricKey,
            //    CreateSynonym,
            //    CreateTable,
            //    CreateTrigger,
            //    CreateType,
            //    CreateUser,
            //    CreateView,
            //    CreateWorkloadGroup,
            //    CreateXmlIndex,
            //    CreateXmlSchemaCollection,
            //    DenyDatabase,
            //    DenyServer,
            //    DropApplicationRole,
            //    DropAssembly,
            //    DropAsymmetricKey,
            //    DropAudit,
            //    DropAvailabilityGroup,
            //    DropBrokerPriority,
            //    DropCertificate,
            //    DropColumnEncryptionKey,
            //    DropColumnMasterKey,
            //    DropContract,
            //    DropCredential,
            //    DropCryptographicProvider,
            //    DropDatabase,
            //    DropDatabaseAuditSpecification,
            //    DropDatabaseEncryptionKey,
            //    DropDefault,
            //    DropEndpoint,
            //    DropEventNotification,
            //    DropEventSession,
            //    DropExtendedProcedure,
            //    DropExtendedProperty,
            //    DropFulltextCatalog,
            //    DropFulltextIndex,
            //    DropFulltextStoplist,
            //    DropFunction,
            //    DropIndex,
            //    DropLinkedServer,
            //    DropLinkedServerLogin,
            //    DropLogin,
            //    DropMasterKey,
            //    DropMessage,
            //    DropMessageType,
            //    DropPartitionFunction,
            //    DropPartitionScheme,
            //    DropProcedure,
            //    DropQueue,
            //    DropRemoteServer,
            //    DropRemoteServiceBinding,
            //    DropResourcePool,
            //    DropRole,
            //    DropRoleMember,
            //    DropRoute,
            //    DropRule,
            //    DropSchema,
            //    DropSearchPropertyList,
            //    DropSecurityPolicy,
            //    DropSequence,
            //    DropServerAudit,
            //    DropServerAuditSpecification,
            //    DropServerRole,
            //    DropServerRoleMember,
            //    DropService,
            //    DropSignature,
            //    DropSignatureSchemaObject,
            //    DropStatistics,
            //    DropSymmetricKey,
            //    DropSynonym,
            //    DropTable,
            //    DropTrigger,
            //    DropType,
            //    DropUser,
            //    DropView,
            //    DropWorkloadGroup,
            //    DropXmlSchemaCollection,
            //    GrantDatabase,
            //    GrantServer,
            //    Rename,
            //    RevokeDatabase,
            //    RevokeServer,
            //    UnbindDefault,
            //    UnbindRule,
            //    UpdateStatistics,
            //}


            // Subscribe to create & drop table events
            _server.Events.SubscribeToEvents(ServerEvent.CreateTable + ServerEvent.DropTable);

            // ctrl + c or ctrl + break to quit
            Console.WriteLine(new string('-', 79));
            Console.WriteLine($@"Starting events for server: {_server.Name}");
            Console.WriteLine(new string('-', 79));
            Console.WriteLine($@"Capturing events {ServerEvent.CreateTable} and {ServerEvent.DropTable}");
            Console.WriteLine(new string('-', 79));
            Console.WriteLine(@"Try creating and droping tables(ctrl+c or ctrl+break to quit)");
            Console.WriteLine(new string('-', 79));


            // Start receiving events
            _server.Events.StartEvents();

            // wait for events 
            while (!_isclosing)
            {
            }
        }

        private static bool _isclosing;


        private static void Events_ServerEvent(object sender, ServerEventArgs e)
        {
            try
            {
                Console.WriteLine(
                    @"EventType: {0, -20} SPID: {1, 4} PostTime: {2, -20}",
                    e.EventType, e.Spid, e.PostTime);

                foreach (var ep in e.Properties)
                {
                    if (ep.Value != null)
                    {
                        Console.WriteLine("\t{0, -30} {1, -30}",
                            ep.Name,
                            ep.Value);
                    }
                    else
                        Console.WriteLine("\t{0,-30}", ep.Name);

                }
                Console.WriteLine();
                Console.WriteLine(new string('-', 79));
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message)));

            }
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {

            switch (ctrlType)
            {
                case CtrlTypes.CtrlCEvent:
                case CtrlTypes.CtrlBreakEvent:
                case CtrlTypes.CtrlCloseEvent:
                    Console.WriteLine("Exiting - Unsubscribe from all the events");
                    _server.Events.UnsubscribeAllEvents();
                    _isclosing = true;
                    break;

            }
            return true;
        }

        #region unmanaged
        // Declare the SetConsoleCtrlHandler function as external and receiving a delegate. 

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        // A delegate type to be used as the handler routine for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes ctrlType);

        // An enumerated type for the control messages sent to the handler routine.
        public enum CtrlTypes
        {
            CtrlCEvent = 0,
            CtrlBreakEvent,
            CtrlCloseEvent,
        }

        #endregion

    }
}
