using Converter.Extension;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DBHelper
{

    #region " Interface definition "

    public interface ILog
    {
        void Log(string text, bool newLine = false);
    }

    #endregion

    public static class DbGeneral
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="logger"></param>
        /// <param name="errMessage"></param>
        /// <param name="bckCertificateName"></param>
        /// <returns></returns>
        public static bool EnsureBackupCertificateExists(ServerConnection cnn,
            ILog logger,
            ref string errMessage,
            string bckCertificateName)
        {
            var certExist = false;
            try
            {
                cnn.Connect();
                var server = new Server(cnn);
                // get the reference to the master database
                var masterDb = server.Databases["master"];

                certExist = masterDb.Certificates[bckCertificateName] != null;

            }
            catch (Exception ex)
            {

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message));

                logger?.Log($"Error in EnsureBackupCertificateExists : {errMessage}");
                logger?.Log(@"..............................................................................................");
            }

            return certExist;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="logger"></param>
        /// <param name="errMessage"></param>
        /// <returns></returns>
        public static bool EnsureMasterKeyExists(ServerConnection cnn,
            ILog logger,
            ref string errMessage)

        {
            var mkExists = false;
            try
            {
                cnn.Connect();
                var server = new Server(cnn);
                // get the reference to the master database
                var masterDb = server.Databases["master"];

                mkExists = masterDb.MasterKey != null;

            }
            catch (Exception ex)
            {

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message));

                logger?.Log($"Error in EnsureMasterKeyExists : {errMessage}");
                logger?.Log(@"..............................................................................................");
            }

            return mkExists;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="logger"></param>
        /// <param name="passwordForMasterKey"></param>
        /// <param name="backupCertificateName"></param>
        /// <param name="errMessage"></param>
        /// <param name="backupKeys"></param>
        /// <param name="privateKeyFile"></param>
        /// <param name="certfile"></param>
        /// <returns></returns>
        public static bool CreateBackupCertificate(ServerConnection cnn,
            ILog logger,
            string passwordForMasterKey,
            string backupCertificateName,
            ref string errMessage,
            bool backupKeys = true,
            string privateKeyFile = @"C:\TMP\SQLPrivateKeyFile",
            string certfile = @"C:\TMP\TestSQLServerCert")

        {
            var retValue = true;
            try
            {
                cnn.Connect();
                var server = new Server(cnn);
                // get the reference to the master database
                var masterDb = server.Databases["master"];

                // Drop certificate and master key
                masterDb.Certificates[backupCertificateName]?.Drop();
                //
                masterDb.MasterKey?.Drop();

                //Create the master key
                var mk = new MasterKey { Parent = masterDb };
                mk.Create(passwordForMasterKey);
                mk.Open(passwordForMasterKey);

                //Creating certificate 
                var certificate = new Certificate(masterDb, backupCertificateName)
                {
                    StartDate = DateTime.Today,
                    Subject = "Backup certificate",
                    ExpirationDate = new DateTime(2100, 12, 31)

                };
                certificate.Create();
                if (backupKeys)
                {
                    if (File.Exists(certfile))
                        File.Delete(certfile);
                    if (File.Exists(privateKeyFile))
                        File.Delete(privateKeyFile);

                    //Create a backup of the server certificate in the master database.  
                    certificate.Export(certfile, privateKeyFile, passwordForMasterKey);
                }


            }
            catch (Exception ex)
            {

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                retValue = false;
                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                    .Select(ex1 => ex1.Message));

                logger?.Log($"Error in CreateBackupCertificate : {errMessage}");
                logger?.Log(@"..............................................................................................");
            }

            return retValue;

        }




        private const string LastBackup = "LastBackup";
        private const string CServer2012 = "11.0.2100.60";
        #region  Backup rutine 

        /// <summary>
        /// BACKUP DATABASE [AdventureWorks2014] TO  DISK = N'D:\bb\dbHelper_AdventureWorks2014_20180223_21_38_37.bak' 
        /// WITH  COPY_ONLY,  DESCRIPTION = N'dbHelper BACKUP AdventureWorks2014', FORMAT, INIT,  NAME = N'COPY ONLY(FULL) dbHelper BACKUP AdventureWorks2014',
        /// SKIP, NOREWIND, NOUNLOAD, COMPRESSION,  STATS = 5, CHECKSUM
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="logger"></param>
        /// <param name="errMessage"></param>
        /// <param name="doVerify"></param>
        /// <param name="useCompression"></param>
        /// <param name="bckEncOpt"></param>
        /// <returns></returns>
        public static bool BackupDatabase(ServerConnection cnn,
                                            ILog logger,
                                            ref string errMessage,
                                            bool doVerify = true,
                                            bool useCompression = true,
                                            BackupEncryptionOptions bckEncOpt = null)
        {
            var retValue = false;
            var isLogging = logger != null;

            Server server;
            Backup source;
            BackupDeviceItem destination;
            Restore restore;
            var dataBaseName = cnn.DatabaseName;
            try
            {
                cnn.Connect();

                server = new Server(cnn);
                string backupFileName =
                    $"{server.BackupDirectory}{(server.BackupDirectory.EndsWith("") ? @"\" : string.Empty)}dbHelper_{dataBaseName}_{DateTime.Now:yyyyMMdd_HH_mm_ss.bak}";

                if (isLogging)
                {
                    logger.Log(@"..............................................................................................");
                    logger.Log($@"BACKUP FILE : {backupFileName}");
                    logger.Log(@"..............................................................................................");
                }

                // Depends on SQL Server Edition
                var canIUseCompression = server.EngineEdition == Edition.EnterpriseOrDeveloper ||
                                         server.EngineEdition == Edition.Standard;


                // instantaniate backup 
                source = new Backup
                {
                    Action = BackupActionType.Database,
                    CopyOnly = true,
                    Checksum = true,
                    Incremental = false,
                    BackupSetDescription = "dbHelper BACKUP " + dataBaseName,
                    BackupSetName = "COPY ONLY(FULL) dbHelper BACKUP " + dataBaseName,
                    ContinueAfterError = false,
                    SkipTapeHeader = true,
                    UnloadTapeAfter = false,
                    NoRewind = true,
                    FormatMedia = true,
                    Initialize = true
                };




                if (useCompression && canIUseCompression)
                    source.CompressionOption = BackupCompressionOptions.On;

                var errorMessage = "";
                // Only for SQL Server 2012+ and Edition diffrent from Express
                var canIUseEncryption = server.EngineEdition != Edition.Express &&
                                        server.Version >= new Version(CServer2012) && bckEncOpt != null &&
                                        bckEncOpt.NoEncryption == false &&
                                        EnsureBackupCertificateExists(cnn, logger, ref errorMessage, bckEncOpt.EncryptorName);
                if (errorMessage != string.Empty)
                    logger?.Log(
                        $"Can not use Encryption because the server certificate does not exists. Additional info : {errorMessage}");


                if (canIUseEncryption)
                {
                    source.EncryptionOption = bckEncOpt;
                }




                source.PercentComplete += (sender, e) =>
                {
                    if (isLogging)
                        logger?.Log($"\tDoing backup : {e.Percent.ToString().Trim()}%");
                };

                source.Complete += (sender, e) =>
                {
                    if (isLogging)
                    {
                        logger?.Log(@"..............................................................................................");
                        logger?.Log(e.Error.Message);
                        logger?.Log(@"..............................................................................................");
                    }
                };


                // setup percent complete notification
                source.PercentCompleteNotification = 5;

                // setup backup destination
                source.Database = dataBaseName;
                destination = new BackupDeviceItem(backupFileName, DeviceType.File);
                source.Devices.Add(destination);

                //------------------------------------------------------------------------------------
                //backup starts here
                //------------------------------------------------------------------------------------
                source.SqlBackup(server);

                if (doVerify)
                {
                    if (isLogging)
                    {
                        logger.Log("CHECKING BACKUP FILE");
                        logger.Log(@"..............................................................................................");
                    }

                    //------------------------------------------------------------------------------------
                    // Verify backup
                    //------------------------------------------------------------------------------------
                    restore = new Restore();
                    restore.Devices.AddDevice(backupFileName, DeviceType.File);
                    restore.Database = dataBaseName;


                    if (restore.SqlVerify(server))
                    {
                        if (isLogging)
                        {
                            logger.Log("The backup file could be used for restoring.Everything seems to be OK!");
                            logger.Log(@"..............................................................................................");
                        }
                    }
                    else
                    {
                        if (isLogging)
                        {
                            logger.Log("The backup file  could not be used for restoring.There are errors in the backup file!");
                            logger.Log(@"..............................................................................................");
                        }
                        retValue = false;
                    }
                }

                var db = server.Databases[dataBaseName];


                // EXEC sys.sp_addextendedproperty @name = N'LastBackup', @value = N'D:\bb\dbHelper_AdventureWorks2014_20180223_21_35_16.bak'
                if (db.ExtendedProperties[LastBackup] == null)
                {
                    var extProperty = new ExtendedProperty
                    {
                        Parent = db,
                        Name = LastBackup,
                        Value = backupFileName
                    };
                    extProperty.Create();
                }
                else
                {
                    db.ExtendedProperties[LastBackup].Value = backupFileName;
                    db.ExtendedProperties[LastBackup].Alter();
                }

                if (cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
                retValue = true;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                retValue = false;

                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                           .Select(ex1 => ex1.Message));


                if (isLogging)
                {
                    logger.Log($"Error during backup : {errMessage}");
                    logger.Log(@"..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                source = null;
                destination = null;
                restore = null;
            }
            return retValue;
        }



        #endregion

        #region  CheckDB 

        public static bool CheckDb(ServerConnection cnn, ILog logger, ref string errMessage)
        {

            var retValue = true;
            var isLogging = logger != null;
            Server server;
            errMessage = string.Empty;
            try
            {
                cnn.Connect();


                server = new Server(cnn);
                var dataBaseName = cnn.DatabaseName;
                Database db = server.Databases[dataBaseName];

                PutDbInUserMode(cnn, DatabaseUserAccess.Single, true, logger, ref errMessage);
                //DBCC CHECKALLOC(N'AdventureWorks2014', REPAIR_ALLOW_DATA_LOSS)  WITH NO_INFOMSGS
                var strings1 = db.CheckAllocations(RepairType.AllowDataLoss);
                //DBCC CHECKCATALOG
                var strings2 = db.CheckCatalog();
                //DBCC CHECKDB(N'AdventureWorks2014', REPAIR_ALLOW_DATA_LOSS)  WITH  NO_INFOMSGS , DATA_PURITY
                var strings3 = db.CheckTables(RepairType.AllowDataLoss, RepairOptions.NoInformationMessages, RepairStructure.DataPurity);


                //ALTER DATABASE [AdventureWorks2014] SET  MULTI_USER WITH ROLLBACK IMMEDIATE
                PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger, ref errMessage);
                db = null;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                retValue = false;


                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                           .Select(ex1 => ex1.Message));

                if (isLogging)
                {
                    logger.Log($"Error during checking database : {errMessage}");
                    logger.Log(@"..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();

                server = null;
            }
            return retValue;

        }
        #endregion

        #region User mode
        /// <summary>
        /// ALTER DATABASE [AdventureWorks2014] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="mode"></param>
        /// <param name="killUserProcess"></param>
        /// <param name="logger"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool PutDbInUserMode(ServerConnection cnn,
                                           DatabaseUserAccess mode,
                                           bool killUserProcess,
                                           ILog logger,
                                           ref string errorMessage)
        {
            var retValue = true;
            Server server;
            var isLogging = logger != null;
            var dbMode = mode;
            var dataBaseName = cnn.DatabaseName;
            Database db;
            try
            {
                server = new Server(cnn);

                if (isLogging)
                {
                    logger.Log($@"Trying to put database in mode : {dbMode}");
                    logger.Log("..............................................................................................");
                }
                db = server.Databases[dataBaseName];
                if (db.UserAccess == dbMode)
                {
                    if (isLogging)
                    {
                        logger.Log($"The database is already in mode : {dbMode}.");
                        logger.Log(@"..............................................................................................");
                    }
                    if (cnn.IsOpen)
                        cnn.Disconnect();
                    server = null;
                    db = null;
                    return true;
                }

                // dbMode is the argument we passed to this routine
                db.UserAccess = dbMode;
                // killUserProcess is passed, kill user process
                if (killUserProcess)
                {
                    KillAllProcessesForOneDatabase(cnn, ref errorMessage);
                }
                // execute Alter 

                db.Alter(TerminationClause.RollbackTransactionsImmediately);
                // after that Refresh is needed
                db.Refresh();
                server.Refresh();
                if (isLogging)
                {
                    logger.Log(
                        $"Successfully accomplished! The database {dataBaseName} has putted in new mode : {dbMode}");
                    logger.Log("..............................................................................................");
                }

            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }



                errorMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                             .Select(ex1 => ex1.Message));

                retValue = false;
                if (isLogging)
                {
                    logger.Log(
                        $"There is an error during changing user access mode on the database : {dataBaseName} new mode : {dbMode}");
                    logger.Log($"The error is : {errorMessage}");
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;
            }

            return retValue;
        }



        #endregion

        #region Kill user process

        public static bool KillAllProcessesForOneDatabase(ServerConnection cnn, ref string errorLog)
        {
            var retValue = true;
            cnn.Connect();

            var server = new Server(cnn);
            string databaseName = cnn.DatabaseName;
            try
            {
                server.KillAllProcesses(databaseName);
                retValue = true;
            }
            catch (Exception ex)
            {
                retValue = false;


                errorLog = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));


            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();

                server = null;
            }

            return retValue;

        }


        #endregion

        #region Determine Log Size

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="errorLog"></param>
        /// <returns></returns>
        public static double DetermineLogSize(ServerConnection cnn, ref string errorLog)
        {
            Server server;
            Database db;
            double retValue = 0;
            var databaseName = cnn.DatabaseName;
            try
            {
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[databaseName];
                retValue = db.LogFiles[0].Size / 1024;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }


                errorLog = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;

                db = null;
            }
            return retValue;
        }


        #endregion

        #region Recovery mode

        /// <summary>
        /// ALTER DATABASE [AdventureWorks2014] SET RECOVERY SIMPLE WITH ROLLBACK IMMEDIATE
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="dbMode"></param>
        /// <param name="killUserProcess"></param>
        /// <param name="logger"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool SetRecoveryMode(
                        ServerConnection cnn,
                        RecoveryModel dbMode,
                        bool killUserProcess,
                        ILog logger,
                        ref string errorMessage
                                            )
        {

            var retValue = true;
            Server server;
            var isLogging = logger != null;
            Database db;
            var dataBaseName = cnn.DatabaseName;
            try
            {
                cnn.Connect();
                server = new Server(cnn);

                if (isLogging)
                {
                    logger.Log($"Setting recovery mode : {dbMode}");
                    logger.Log(@"..............................................................................................");
                }
                db = server.Databases[dataBaseName];
                if (db.RecoveryModel == dbMode)
                {
                    if (isLogging)
                    {
                        logger.Log($"The database is already in required mode : {dbMode}.");
                        logger.Log("..............................................................................................");
                    }
                    if (cnn.IsOpen)
                        cnn.Disconnect();
                    server = null;
                    db = null;
                    return true;
                }
                db.RecoveryModel = dbMode;
                if (killUserProcess)
                {
                    KillAllProcessesForOneDatabase(cnn, ref errorMessage);
                    db.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
                db.Refresh();
                server.Refresh();
                if (isLogging)
                {
                    logger.Log(
                        $"Successfully complited. The database : {dataBaseName} has been putted in recovery mode : {dbMode}");
                    logger.Log("..............................................................................................");
                }

            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();


                errorMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));

                retValue = false;
                if (isLogging)
                {
                    logger.Log(
                        $"There is an error during operation setting database : {dataBaseName} into new mode : {dbMode}");
                    logger.Log($"The error is  {errorMessage}");
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;
            }

            return retValue;
        }

        #endregion

        #region Restore database

        /// <summary>
        /// RESTORE DATABASE [AdventureWorks2014] FROM  
        /// DISK = N'D:\bb\dbHelper_AdventureWorks2014_20180223_21_46_01.bak' WITH  RESTRICTED_USER,  
        /// NOUNLOAD,  REPLACE,  STATS = 5
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="logger"></param>
        /// <param name="errMessage"></param>
        /// <returns></returns>
        public static bool RestoreDatabase(ServerConnection cnn, ILog logger, ref string errMessage)
        {
            var retValue = false;
            Server server;
            Restore destination;
            BackupDeviceItem source;
            var isLogging = logger != null;
            Database db1;
            string dataBaseName = cnn.DatabaseName;
            try
            {
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log($"Restoring the database {dataBaseName}");
                    logger.Log("..............................................................................................");
                }
                cnn.Connect();

                server = new Server(cnn);

                Database db = server.Databases[dataBaseName];

                string backupFileName;
                if (db.ExtendedProperties[LastBackup] == null)
                {
                    errMessage = "There is no information about last backup!!!";
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log(errMessage);
                        logger.Log("..............................................................................................");
                    }
                    return false;
                }
                else
                {
                    backupFileName = db.ExtendedProperties[LastBackup].Value.ToString();

                }


                destination = new Restore
                {
                    PercentCompleteNotification = 5,
                    RestrictedUser = true
                };

                destination.PercentComplete += (sender, e) =>
                {
                    if (isLogging)
                    {
                        logger?.Log($"\tRestoring : {e.Percent.ToString().Trim()}%");
                    }

                };

                destination.Complete += (sender, e) =>
                {
                    if (isLogging)
                    {
                        logger?.Log("..............................................................................................");
                        logger?.Log(e.Error.Message);
                        logger?.Log("..............................................................................................");
                    }
                };


                destination.Action = RestoreActionType.Database;
                destination.Database = dataBaseName;
                source = new BackupDeviceItem(backupFileName, DeviceType.File);
                destination.Devices.Add(source);
                destination.ReplaceDatabase = true;
                destination.NoRecovery = false;


                server.KillAllProcesses(dataBaseName);


                db.UserAccess = DatabaseUserAccess.Single;
                db.Alter(TerminationClause.RollbackTransactionsImmediately);
                server.DetachDatabase(dataBaseName, false);


                destination.SqlRestore(server);
                server.Databases.Refresh();
                if (cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
                cnn.Connect();
                server = new Server(cnn);
                db1 = server.Databases[dataBaseName];
                db1.UserAccess = DatabaseUserAccess.Multiple;
                db1.Alter(TerminationClause.RollbackTransactionsImmediately);
                if (db1.ExtendedProperties[LastBackup] == null)
                {
                    var extProperty = new ExtendedProperty
                    {
                        Parent = db1,
                        Name = LastBackup,
                        Value = backupFileName
                    };
                    extProperty.Create();
                }
                else
                {
                    ExtendedProperty extProperty = db1.ExtendedProperties[LastBackup];
                    extProperty.Value = backupFileName;
                    extProperty.Alter();
                }
                db1.Refresh();
                retValue = true;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                retValue = false;


                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));

                if (isLogging)
                {
                    logger.Log($"Error during restoring : {errMessage}");
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                source = null;
                destination = null;
                db1 = null;
            }
            return retValue;
        }


        #endregion

        #region Shrink Log
        /// <summary>
        /// DBCC SHRINKFILE(N'AdventureWorks2014_Log' , 256, TRUNCATEONLY)
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="targetLogFileSizeMb"></param>
        /// <param name="logger"></param>
        /// <param name="errMessage"></param>
        /// <returns></returns>
        public static bool ShrinkLog(ServerConnection cnn, int targetLogFileSizeMb, ILog logger, ref string errMessage)
        {
            var retValue = true;
            var isLogging = logger != null;
            Server server;
            Database db;
            var dataBaseName = cnn.DatabaseName;
            try
            {
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[dataBaseName];


                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log($"Recovery model {db.RecoveryModel} Shrinking log ");
                    logger.Log("..............................................................................................");
                }

                double before = db.LogFiles[0].Size / 1024;
                if (isLogging)
                {
                    logger.Log($"The log size before shrinking is : {before} in MB ");
                    logger.Log("..............................................................................................");
                }

                db.LogFiles[0].Shrink(targetLogFileSizeMb, ShrinkMethod.TruncateOnly);

                db.LogFiles[0].Refresh();
                if (isLogging)
                {
                    logger.Log("The log has been shrinked! ");
                    logger.Log("..............................................................................................");
                }
                double after = db.LogFiles[0].Size / 1024;

                if (isLogging)
                {
                    logger.Log($"The log size after shrinking : {after} in MB ");
                    logger.Log("..............................................................................................");
                    logger.Log($"The log has been reduced {(before - after)} in MB");
                }
                if (cnn.IsOpen)
                    cnn.Disconnect();
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                retValue = false;


                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));

                if (isLogging)
                {
                    logger.Log($"There is an error during shrinking : {errMessage}");
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                db = null;
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
            }
            return retValue;
        }


        #endregion

        #region Force log shrinking

        /// <summary>
        /// Switches the database to a simple recovery mode if needed
        /// Performs a backup
        /// Performs a shrink log
        /// Switches the database to a full recovery mode if it was previously in the same
        /// Performs a backup
        /// </summary>
        /// <param name="cnn">connection to the server instance</param>
        /// <param name="targetLogSizeInMb">target log size in MB</param>
        /// <param name="logger"></param>
        /// <param name="errorMessage">capture the error message</param>
        /// <returns></returns>
        public static bool ForceShrinkingLog(ServerConnection cnn, int targetLogSizeInMb, ILog logger, ref string errorMessage)
        {

            var retValue = true;
            Server server;
            var isLogging = logger != null;
            Database db;
            var changeMode = false;
            var dataBaseName = cnn.DatabaseName;
            errorMessage = string.Empty;
            try
            {
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[dataBaseName];


                if (db.RecoveryModel == RecoveryModel.Full)
                {
                    changeMode = true;
                    if (SetRecoveryMode(cnn, RecoveryModel.Simple, true, logger, ref errorMessage) == false)
                    {
                        throw new Exception("I'm not able to put database in the simple recovery mode");
                    }

                    if (BackupDatabase(cnn, logger, ref errorMessage) == false)
                    {
                        throw new Exception("I'm not able to perform the database backup!");
                    }
                }
                //DBCC SHRINKFILE(N'AdventureWorks2014_Log' , 256, TRUNCATEONLY)
                if (!ShrinkLog(cnn, targetLogSizeInMb, logger, ref errorMessage))
                {
                    throw new Exception("I’m not able to shrink the database log file!");
                }
                if (changeMode)
                {
                    if (SetRecoveryMode(cnn, RecoveryModel.Full, true, logger, ref errorMessage) == false)
                    {
                        throw new Exception("I'm not able to put the database in full recovery mode");
                    }
                    if (BackupDatabase(cnn, logger, ref errorMessage) == false)
                    {
                        throw new Exception("I'm not able to perform backup!");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }


                errorMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));

                retValue = false;
                if (isLogging)
                {
                    logger.Log($"The error is  : {errorMessage}");
                    logger.Log("..............................................................................................");
                }
            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;
            }
            return retValue;
        }


        #endregion



        #region " Is the database online? "
        /// <summary>
        /// T-SQL equivalent 
        /// -------------------------------
        /// SELECT
        /// CASE
        /// WHEN HAS_DBACCESS(N'AdventureWorks2014') = 1 THEN 'true'
        /// ELSE 'false'
        /// END
        /// -------------------------------
        /// Remarks
        /// HAS_DBACCESS returns 1 if the user has access to the database, 0 if the user has no access to the database, and NULL if the database name is not valid.
        /// HAS_DBACCESS returns 0 if the database is offline or suspect.
        /// HAS_DBACCESS returns 0 if the database is in single-user mode and the database is in use by another user.
        /// </summary>
        /// <param name="cnn">ServerConnection connection</param>
        /// <param name="logger">Do we capture errors & informations</param>
        /// <param name="errMessage"></param>
        /// <returns>True - user has access to the database
        /// false if the database does not exists or if the database is offline
        /// </returns>
        public static bool IsTheDataBaseOnLine(ServerConnection cnn, ILog logger, ref string errMessage)
        {
            var retValue = true;
            var isLogging = logger != null;
            var dataBaseName = cnn.DatabaseName;
            var server = new Server(cnn.ServerInstance);
            var db = server.Databases[dataBaseName];
            errMessage = string.Empty;
            try
            {
                cnn.Connect();

                if (db.Status == DatabaseStatus.Normal)
                {
                    logger?.Log("..............................................................................................");
                    logger?.Log($"The database \"{dataBaseName}\" is ONLINE!");
                    logger?.Log("..............................................................................................");
                }
                else
                {
                    retValue = false;
                    logger?.Log("..............................................................................................");
                    logger?.Log($"The database \"{dataBaseName}\" is NOT ONLINE");
                    logger?.Log("..............................................................................................");
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }



                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                           .Select(ex1 => ex1.Message));

                if (retValue == false)
                {
                    if (isLogging)
                    {
                        logger.Log($"The database \"{dataBaseName}\" does not exists!");
                        logger.Log($"Exception : {errMessage}");
                        logger.Log("..............................................................................................");
                    }
                }
            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
            }

            return retValue;
        }

        #endregion

        #region Is there any backup
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="logger"></param>
        /// <param name="errorLog"></param>
        /// <returns></returns>
        public static bool IsThereAnyBackupTaken(ServerConnection cnn, ILog logger, ref string errorLog)
        {
            var result = true;
            Server server;
            Database db;
            try
            {
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[cnn.DatabaseName];
                int numberOfBackupFiles = 0;
                long totalSizeInBytes = 0;
                if ((db.ExtendedProperties[LastBackup] == null))
                {
                    result = false;
                    errorLog = "Backup via dbHelper has not done";
                }
                else
                {
                    var backupExists = DetermineNumberOfBackupFiles(cnn, logger, ref numberOfBackupFiles, ref totalSizeInBytes, ref errorLog);
                    if (backupExists == false)
                    {
                        result = false;
                        errorLog = "The backup file was deleted!";
                    }


                }

            }
            catch (Exception ex)
            {


                errorLog = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                            .Select(ex1 => ex1.Message));

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;
            }
            return result;


        }
        #endregion

        public enum DiskSizeUnit
        {
            Bytes = 0,
            KiloBytes = 1,
            MegaBytes = 2,
            GigaBytes = 3,
            TeraBytes = 4
        }


        #region "Can I perform a backup? "

        public static double DetermineSize(double dataBaseSize, DiskSizeUnit diskSizeUnit)
        {
            if (diskSizeUnit > DiskSizeUnit.MegaBytes)
            {
                var dividedBy = Math.Pow(1024, (int)diskSizeUnit-(int)(DiskSizeUnit.MegaBytes));

                dataBaseSize /= dividedBy;
            }
            else if (diskSizeUnit < DiskSizeUnit.MegaBytes)
            {
                var multiplyBy = Math.Pow(1024, (int)diskSizeUnit+(int)DiskSizeUnit.MegaBytes);
                dataBaseSize *= multiplyBy;
            }

            return dataBaseSize;
        }

        public static bool CanIPerformABackup(ServerConnection cnn, ILog logger, ref string errMessage,
            DiskSizeUnit unitOfMeasure = DiskSizeUnit.MegaBytes)
        {


            bool retValue;
            Database db;
            Server server;
            var isLogging = logger != null;
            errMessage = string.Empty;
            try
            {
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("I'm checking free disk space...");
                    logger.Log("..............................................................................................");

                }

                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[cnn.DatabaseName];
                var dataBaseSize = db.Size;
                if (unitOfMeasure != DiskSizeUnit.MegaBytes)
                {
                    dataBaseSize = DetermineSize(dataBaseSize, unitOfMeasure);
                }

                double freeToUse = 0;
                var driveLetterToCheck = server.BackupDirectory.Substring(0, 1);

                const string command = @"DECLARE	@freeSpace float
                                   DECLARE @errorMessage nvarchar(1000)
                                   EXEC [IOHELPER].[FreeSpace] @driveLetter = N'{0}',@unitOfMeasure={1}, @freeSpace = @freeSpace OUTPUT, @errorMessage = @errorMessage OUTPUT;
                                   SELECT @freeSpace AS N'@freeSpace', @errorMessage AS N'@errorMessage'";


                var ds = db.ExecuteWithResults(string.Format(command, driveLetterToCheck, (int)unitOfMeasure));

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    freeToUse = (double)ds.Tables[0].Rows[0]["@freeSpace"];


                if ((freeToUse > dataBaseSize))
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log(
                            $"Free on disk {driveLetterToCheck} in {Enum.Parse(typeof(DiskSizeUnit), unitOfMeasure.ToString())} is : {freeToUse} , and the expected backup size is  {dataBaseSize:N8} in {Enum.Parse(typeof(DiskSizeUnit), unitOfMeasure.ToString())}");
                        logger.Log("..............................................................................................");
                    }
                    retValue = true;
                }
                else
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log(
                            $"There is no enough space on drive {driveLetterToCheck} in {Enum.Parse(typeof(DiskSizeUnit), unitOfMeasure.ToString())} is : {freeToUse} , and the expected backup size is  {dataBaseSize:N8} in {Enum.Parse(typeof(DiskSizeUnit), unitOfMeasure.ToString())}");
                        logger.Log("..............................................................................................");
                    }
                    retValue = false;
                }

            }
            catch (Exception ex)
            {
                retValue = false;


                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log(
                        $"An error occures while trying to determine free disk space. The error message :{errMessage}");
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;

            }

            return retValue;
        }
        #endregion



        #region " Delete old backup files "

        public static bool DeleteBackupFiles(ServerConnection cnn, ILog logger, ref string errMessage)
        {
            var retValue = true;
            Database db;
            Server server;
            errMessage = string.Empty;
            var isLogging = logger != null;
            try
            {
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("I'm trying to delete old backup files..");
                    logger.Log("..............................................................................................");

                }
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[cnn.DatabaseName];
                var dataBaseName = cnn.DatabaseName;

                string filter = $"dbHelper_{dataBaseName}_*.BAK";
                var backupDirectory = server.BackupDirectory;
                if (server.BackupDirectory.EndsWith(@"\") == false)
                    backupDirectory += @"\";

                // setup calling the stored procedre. Notice the procedure is the result of publishing SQLCLR project 
                var command = @"DECLARE @errorMessage nvarchar(1000)
                                   EXEC [IOHELPER].[DeleteFiles] @path = N'{0}', @filter = N'{1}', @errorMessage = @errorMessage OUTPUT;
                                   SELECT @errorMessage AS N'@errorMessage'";
                // Execute stored procedure and return errormessage if any 
                DataSet ds = db.ExecuteWithResults(String.Format(command, backupDirectory, filter));

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["@errorMessage"].ToString().Trim() != string.Empty)
                    errMessage = (string)ds.Tables[0].Rows[0]["@errorMessage"];


                if (errMessage.Trim() == string.Empty)
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log(
                            $"I have been deleted in directory : {backupDirectory} files with following pattern {filter}");
                        logger.Log("..............................................................................................");
                    }
                    db.ExtendedProperties[LastBackup]?.Drop();

                    retValue = true;
                }
                else
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log(
                            $@"There is an error when trying to delete old backup files in directory : {
                                backupDirectory
                            } files with following patther {filter}Error description : {errMessage}");
                        logger.Log("..............................................................................................");
                    }
                    retValue = false;
                }

            }
            catch (Exception ex)
            {
                retValue = false;


                errMessage = string.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("An error occures while trying to delete old backup files. The error message :" + errMessage);
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;

            }

            return retValue;
        }
        #endregion


        #region " Determine the number of backup files "

        public static bool DetermineNumberOfBackupFiles(
                                    ServerConnection cnn,
                                    ILog logger,
                                    ref int numberOfFiles,
                                    ref long totalSizeInBytes,
                                    ref string errorMessage)
        {
            var backupExists = true;
            Database db;
            Server server;
            var checkIfExist = false;
            var isLogging = logger != null;
            numberOfFiles = 0;
            totalSizeInBytes = 0;
            errorMessage = string.Empty;

            try
            {
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("I'm trying to determine how many backup files exists..");
                    logger.Log("..............................................................................................");

                }
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[cnn.DatabaseName];
                var dataBaseName = cnn.DatabaseName;
                var lastValidBackup = string.Empty;


                if (db.ExtendedProperties[LastBackup] != null)
                {
                    checkIfExist = true;
                    var extProperty = db.ExtendedProperties[LastBackup];
                    lastValidBackup = extProperty.Value.ToString();
                }


                var filter = $"dbHelper_{dataBaseName}_*.BAK";
                var backupDirectory = server.BackupDirectory;
                if (server.BackupDirectory.EndsWith(@"\") == false)
                    backupDirectory += @"\";

                string command = @"SELECT * FROM [IOHELPER].[FileHelper] (N'{0}',N'{1}')";

                var ds = db.ExecuteWithResults(string.Format(command, backupDirectory, filter));
                if (ds.Tables.Count > 0)
                {
                    numberOfFiles = ds.Tables[0].Rows.Count;
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        if (checkIfExist && r["Name"].ToString().Equals(lastValidBackup))
                            backupExists = true;

                        totalSizeInBytes += (long)r["SizeInBytes"];
                    }
                }

            }
            catch (Exception ex)
            {
                backupExists = false;


                errorMessage = String.Join(Environment.NewLine + "\t", ex.CollectThemAll(ex1 => ex1.InnerException)
                      .Select(ex1 => ex1.Message));
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log(
                        $"An error occures while trying to determine how many backup file are taken. The error message :{errorMessage}");
                    logger.Log("..............................................................................................");
                }
            }
            finally
            {
                if (cnn.IsOpen)
                    cnn.Disconnect();
                server = null;
                db = null;
            }



            return backupExists;
        }

        #endregion

    }


}
