using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Data;

/// <summary>
/// T-SQL EQUIVALENT ZA SVAKI DIO 
/// WIN.FORMS TESTER 
/// SCRIPTER 
/// ALTER TABLE ADD COLUMN
/// </summary>
namespace DBHelper
{

    #region " Interface definition "

    public interface ILog
    {
        void Log(string text, bool newLine=false);
    }

    #endregion

    public static class DBGeneral
    {
        public const string LAST_BACKUP = "LastBackup";

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
        /// <returns></returns>
        public static bool BackupDatabase(ServerConnection cnn, ILog logger,  ref string errMessage, bool doVerify = true)
        {
            bool retValue = false;
            bool isLogging = logger == null ? false : true;

            Server server = null;
            Backup source = null;
            BackupDeviceItem destination = null;
            Restore restore = null;
            string dataBaseName = cnn.DatabaseName;
            string errMesage = string.Empty;
            try
            {
                cnn.Connect();

                server = new Server(cnn);
                string backupFileName = server.BackupDirectory + 
                    (server.BackupDirectory.EndsWith("") ? @"\" : string.Empty) 
                    + "dbHelper_" 
                    + dataBaseName 
                    + "_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss.bak");

                if ( isLogging)
                { 
                    logger.Log(@"..............................................................................................");
                    logger.Log(@"BACKUP FILE : " + backupFileName );
                    logger.Log(@"..............................................................................................");
                }

                source = new Backup();
                source.Action = BackupActionType.Database;
                source.CopyOnly = true;
                source.Checksum = true;

                source.Incremental = false;
                source.BackupSetDescription = "dbHelper BACKUP " + dataBaseName;
                source.BackupSetName = "COPY ONLY(FULL) dbHelper BACKUP " + dataBaseName;
                source.ContinueAfterError = false;
                source.CompressionOption = BackupCompressionOptions.On;
                source.SkipTapeHeader = true;
                source.UnloadTapeAfter = false;
                source.NoRewind = true;
                source.FormatMedia = true;
                source.Initialize = true;



                source.PercentComplete += (object sender, PercentCompleteEventArgs e) =>
                {
                    if (isLogging)
                        logger.Log("\t" + "Doing backup : " + e.Percent.ToString().Trim() + "%" );
                };

                source.Complete += (object sender, ServerMessageEventArgs e) =>
                {
                    if (isLogging)
                    {
                        logger.Log(@".............................................................................................." );
                        logger.Log(e.Error.Message);
                        logger.Log(@".............................................................................................." );
                    }
                };



                source.PercentCompleteNotification = 5;


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
                        logger.Log(@".............................................................................................." );
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
                            logger.Log(@".............................................................................................." );
                        }
                    }
                    else
                    {
                        if (isLogging)
                        {
                            logger.Log("The backup file  could not be used for restoring.There are errors in the backup file!");
                            logger.Log(@".............................................................................................." );
                        }
                        retValue = false;
                    }
                }

                Database db = server.Databases[dataBaseName];


                // EXEC sys.sp_addextendedproperty @name = N'LastBackup', @value = N'D:\bb\dbHelper_AdventureWorks2014_20180223_21_35_16.bak'
                if (db.ExtendedProperties[LAST_BACKUP] == null)
                {
                    ExtendedProperty extProperty = new ExtendedProperty();
                    extProperty.Parent = db;
                    extProperty.Name = LAST_BACKUP;
                    extProperty.Value = backupFileName;
                    extProperty.Create();
                }
                else
                {
                    ExtendedProperty extProperty = db.ExtendedProperties[LAST_BACKUP];
                    extProperty.Value = backupFileName;
                    extProperty.Alter();
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
                errMessage = ex.Message;
                if (isLogging)
                {
                    logger.Log("Error during backup : " + errMessage );
                    if (ex.InnerException != null)
                        logger.Log("Error during backup : " + ex.InnerException.Message );
                    logger.Log(@".............................................................................................." );
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
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
            bool retValue = true;
            bool isLogging = logger == null ? false : true;
            Server server = null;
            string dataBaseName = null;
            errMessage = string.Empty;
            try
            {
                cnn.Connect();


                server = new Server(cnn);
                dataBaseName = cnn.DatabaseName;
                Database db = server.Databases[dataBaseName];
                ///-------------------------------------------
                ///put database in single user mode 
                ///..........................................
                ///
                PutDbInUserMode(cnn, DatabaseUserAccess.Single, true, logger,ref errMessage);
                //DBCC CHECKALLOC(N'AdventureWorks2014', REPAIR_ALLOW_DATA_LOSS)  WITH NO_INFOMSGS
                StringCollection strings1 = db.CheckAllocations(RepairType.AllowDataLoss);
                //DBCC CHECKCATALOG
                StringCollection strings2 = db.CheckCatalog();
                //DBCC CHECKDB(N'AdventureWorks2014', REPAIR_ALLOW_DATA_LOSS)  WITH  NO_INFOMSGS , DATA_PURITY
                StringCollection strings3 = db.CheckTables(RepairType.AllowDataLoss, RepairOptions.NoInformationMessages, RepairStructure.DataPurity);


                //ALTER DATABASE [AdventureWorks2014] SET  MULTI_USER WITH ROLLBACK IMMEDIATE
                PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true, logger,  ref errMessage);
                /// TO DO DISPLAY result 
                db = null;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                retValue = false;
                errMessage = ex.Message;
                if (isLogging)
                {
                    logger.Log("Error during checking database : " + errMessage );
                    if (ex.InnerException != null)
                        logger.Log("Error during checking database : " + ex.InnerException.Message );
                    logger.Log(@".............................................................................................." );
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
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
            bool retValue = true;
            Server server = null;
            bool isLogging = logger == null ? false : true;
            DatabaseUserAccess dbMode = mode;
            string dataBaseName = cnn.DatabaseName;
            Database db = null;
            try
            {
                server = new Server(cnn);

                if (isLogging)
                {
                    logger.Log(@"Trying to put database in mode : " + dbMode.ToString() );
                    logger.Log("..............................................................................................");
                }
                db = server.Databases[dataBaseName];
                if (db.UserAccess == dbMode)
                {
                    if (isLogging)
                    {
                        logger.Log("The database is already in mode : " + dbMode.ToString() + "." );
                        logger.Log(@".............................................................................................." );
                    }
                    if (cnn.IsOpen)
                    {
                        cnn.Disconnect();
                    }
                    server = null;
                    db = null;
                    return retValue;
                }
                db.UserAccess = dbMode;
                if (killUserProcess)
                {
                    KillAllProcessesForOneDatabase(cnn, ref errorMessage);
                    db.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
                db.Refresh();
                server.Refresh();
                if (isLogging)
                {
                    logger.Log("Successfully accomplished! The database " + dataBaseName + " has putted in new mode : " + dbMode.ToString() );
                    logger.Log("..............................................................................................");
                }

            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                errorMessage = ex.Message;
                if ( ex.InnerException != null)
                    errorMessage +=  "\t" +  ex.InnerException.Message;

                retValue = false;
                if (isLogging)
                {
                    logger.Log("There is an error during changing user access mode on the database : " + dataBaseName + " new mode : " + dbMode.ToString() );
                    logger.Log("The error is : " + errorMessage );
                    logger.Log(".............................................................................................." );
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
                server = null;
                db = null;
            }

            return retValue;
        }



        #endregion

        #region Kill user process

        public static bool KillAllProcessesForOneDatabase(ServerConnection cnn, ref string errorLog)
        {
            bool retValue = true;
            cnn.Connect();

            Server Server = new Server(cnn);
            string databaseName = cnn.DatabaseName;
            try
            {
                Server.KillAllProcesses(databaseName);
                retValue = true;
            }
            catch (Exception ex)
            {
                retValue = false;
                errorLog = ex.Message; 
                if (ex.InnerException != null )
                    errorLog += "\r\n" +  ex.InnerException.Message;
            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
             
                Server = null;
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
            Server server = null;
            Database db = null;
            double retValue = 0;
            string databaseName = cnn.DatabaseName;
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
                errorLog = ex.Message;
                if (ex.InnerException != null)
                    errorLog += "\r\n" + ex.InnerException.Message;
               
            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
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

            bool retValue = true;
            Server server = null;
            bool isLogging = logger == null ? false : true;
            Database db = null;
            string dataBaseName = cnn.DatabaseName;
            try
            {
                cnn.Connect();
                server = new Server(cnn);

                if (isLogging)
                {
                    logger.Log("Setting recovery mode : " + dbMode.ToString() );
                    logger.Log(@".............................................................................................." );
                }
                db = server.Databases[dataBaseName];
                if (db.RecoveryModel == dbMode)
                {
                    if (isLogging)
                    {
                        logger.Log("The database is already in required mode : " + dbMode.ToString() + "." );
                        logger.Log(".............................................................................................." );
                    }
                    if (cnn.IsOpen)
                    {
                        cnn.Disconnect();
                    }
                    server = null;
                    db = null;
                    return retValue;
                }
                db.RecoveryModel = dbMode;
                if (killUserProcess)
                {
                    KillAllProcessesForOneDatabase( cnn, ref errorMessage );
                    db.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
                db.Refresh();
                server.Refresh();
                if (isLogging)
                {
                    logger.Log("Successfully complited. The database : " + dataBaseName + " has been putted in recovery mode : " + dbMode.ToString() );
                    logger.Log(".............................................................................................." );
                }

            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\r\n" + ex.InnerException.Message;
                
                retValue = false;
                if (isLogging)
                {
                    logger.Log("There is an error during operation setting database : " + dataBaseName + " into new mode : " + dbMode.ToString() );
                    logger.Log("The error is  " + errorMessage );
                    logger.Log(".............................................................................................." );
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
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
            bool retValue = false;
            Server server = null;
            Restore destination = null;
            BackupDeviceItem source = null;
            bool isLogging = logger == null ? false : true;
            string backupFileName = string.Empty;
            Database db1 = null;
            string dataBaseName = cnn.DatabaseName;
            try
            {
                if (isLogging)
                {
                    logger.Log(".............................................................................................." );
                    logger.Log("Restoring the database " + dataBaseName );
                    logger.Log(".............................................................................................." );
                }
                cnn.Connect();

                server = new Server(cnn);

                Database db = server.Databases[dataBaseName];

                if (db.ExtendedProperties[LAST_BACKUP] == null)
                {
                    errMessage = "There is no information about last backup!!!";
                    if (isLogging)
                    {
                        logger.Log(".............................................................................................." );
                        logger.Log(errMessage );
                        logger.Log(".............................................................................................." );
                    }
                    return false;
                }
                else
                {
                    ExtendedProperty extProperty = db.ExtendedProperties[LAST_BACKUP];
                    backupFileName = extProperty.Value.ToString();

                }


                destination = new Restore();
                destination.PercentCompleteNotification = 5;

                destination.RestrictedUser = true;
                destination.PercentComplete += (object sender, PercentCompleteEventArgs e) =>
                {
                    if (isLogging)
                    {
                        logger.Log("\t" + "Restoring : " + e.Percent.ToString().Trim() + "%" );
                    }

                };

                destination.Complete += (object sender, ServerMessageEventArgs e) =>
                {
                    if (isLogging)
                    {
                        logger.Log(".............................................................................................." );
                        logger.Log(e.Error.Message );
                        logger.Log(".............................................................................................." );
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
                if (db1.ExtendedProperties[LAST_BACKUP] == null)
                {
                    ExtendedProperty extProperty = new ExtendedProperty();
                    extProperty.Parent = db1;
                    extProperty.Name = LAST_BACKUP;
                    extProperty.Value = backupFileName;
                    extProperty.Create();
                }
                else
                {
                    ExtendedProperty extProperty = db1.ExtendedProperties[LAST_BACKUP];
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
                errMessage = ex.Message;
                if (ex.InnerException != null)
                    errMessage += "\r\n" + ex.InnerException.Message;

                if (isLogging)
                {
                    logger.Log("Error during restoring : " + errMessage );
                    if (ex.InnerException != null)
                        logger.Log("Error during restore operation : " + ex.InnerException.Message );
                    logger.Log(".............................................................................................." );
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
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
        /// <param name="TargetLogFileSizeMB"></param>
        /// <param name="logger"></param>
        /// <param name="errMessage"></param>
        /// <returns></returns>
        public static bool ShrinkLog(ServerConnection cnn,  int TargetLogFileSizeMB,ILog logger,  ref string errMessage)
        {
            bool retValue = true;
            bool isLogging = logger == null ? false : true;
            Server server = null;
            Database db = null;
            string dataBaseName = cnn.DatabaseName;
            try
            {
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[dataBaseName];


                if (isLogging)
                {
                    logger.Log(".............................................................................................." );
                    logger.Log("Recovery model " + db.RecoveryModel.ToString() + " Shrinking log " );
                    logger.Log(".............................................................................................." );
                }

                double before = db.LogFiles[0].Size / 1024;
                if (isLogging)
                {
                    logger.Log("The log size before shrinking is : " + before.ToString() + " in MB " );
                    logger.Log(".............................................................................................." );
                }

                db.LogFiles[0].Shrink(TargetLogFileSizeMB, ShrinkMethod.TruncateOnly);

                db.LogFiles[0].Refresh();
                if (isLogging)
                {
                    logger.Log("The log has been shrinked! " );
                    logger.Log(".............................................................................................." );
                }
                double after = db.LogFiles[0].Size / 1024;

                if (isLogging)
                {
                    logger.Log("The log size after shrinking : " + after.ToString() + " in MB " );
                    logger.Log(".............................................................................................." );
                    logger.Log("The log has been reduced " + (before - after).ToString() + " in MB" );
                }
                if (cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                retValue = false;
                errMessage = ex.Message;
                if (ex.InnerException != null )
                    errMessage += "\r\n" +  ex.InnerException.Message;

                if (isLogging)
                {
                    logger.Log("There is an error during shrinking : " + ex.Message );
                    logger.Log(".............................................................................................." );
                }

            }
            finally
            {
                db = null;
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
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
        /// <param name="targetLogSizeInMB">target log size in MB</param>
        /// <param name="killUserProcess">do we kill user process before</param>
        /// <param name="logger"></param>
        /// <param name="errorMessage">capture the error message</param>
        /// <returns></returns>
        public static bool ForceShrinkingLog(ServerConnection cnn, int targetLogSizeInMB, bool killUserProcess, ILog logger,  ref string errorMessage)
        {

            bool retValue = true;
            Server server = null;
            bool isLogging = logger == null ? false : true;
            Database db = null;
            bool changeMode = false;
            string dataBaseName = cnn.DatabaseName;
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
                  
                    if (BackupDatabase( cnn, logger, ref errorMessage,true) == false)
                    {
                        throw new Exception("I'm not able to perform the database backup!");
                    }
                }
                //DBCC SHRINKFILE(N'AdventureWorks2014_Log' , 256, TRUNCATEONLY)
                if (!ShrinkLog(cnn, targetLogSizeInMB, logger,  ref errorMessage ))
                {
                    throw new Exception("I’m not able to shrink the database log file!");
                }
                if (changeMode)
                {
                    if (SetRecoveryMode(cnn, RecoveryModel.Full, true, logger,  ref errorMessage) == false)
                    {
                        throw new Exception("I'm not able to put the database in full recovery mode");
                    }
                    string backupFileName = string.Empty;
                    if (BackupDatabase(cnn, logger,  ref errorMessage, true) == false)
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
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\r\n" + ex.InnerException.Message;

                retValue = false;
                if (isLogging)
                {
                    logger.Log("The error is  : " + errorMessage );
                    logger.Log(".............................................................................................." );
                }
            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
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
        public static bool IsTheDataBaseOnLine(ServerConnection cnn, ILog logger, ref string errMessage )
        {
            bool retValue = true;
            bool isLogging = logger == null ? false : true;
            string dataBaseName = cnn.DatabaseName;
            Server server = new Server(cnn.ServerInstance);
            Database db = server.Databases[dataBaseName];
            errMessage = string.Empty;
            try
            {
                cnn.Connect(); 

                if (db.Status == DatabaseStatus.Normal)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("The database \"" + dataBaseName + "\" is ONLINE!");
                    logger.Log("..............................................................................................");
                }
                else
                {
                    retValue = false;
                    logger.Log("..............................................................................................");
                    logger.Log("The database \"" + dataBaseName + "\" is NOT ONLINE");
                    logger.Log("..............................................................................................");
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                errMessage = ex.Message;
                if (retValue == false)
                {
                    if (isLogging)
                    {
                        logger.Log("The database \"" + dataBaseName + "\" does not exists!");
                        logger.Log("Exception : " + ex.Message);
                        logger.Log("..............................................................................................");
                    }
                }
            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
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
        public static bool IsThereAnyBackupTaken(ServerConnection cnn, ILog logger,  ref string errorLog)
        {
            bool result = true;
            Server server = null;
            Database db = null;
            ExtendedProperty extProperty = null;
            try
            {
                cnn.Connect();
                server = new Server(cnn);
                db = server.Databases[cnn.DatabaseName];
                if ((db.ExtendedProperties[LAST_BACKUP] == null))
                {
                    result = false;
                    errorLog = "Backup via dbHelper has not done";
                }
                else
                {
                    extProperty = db.ExtendedProperties[LAST_BACKUP];
                    
                    int numberOfBackupFiles =0;
                    Int64 totalSizeInBytes = 0;
                    bool backupExists = DetermineNumberOfBackupFiles(cnn, logger, ref numberOfBackupFiles, ref totalSizeInBytes,ref errorLog);
                    if ( backupExists == false )
                    {
                        result = false;
                        errorLog = "The backup file was deleted!";
                    }
                   

                }

            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
                server = null;
                db = null;
            }
            return result;


        }
        #endregion



        #region "Can I perform a backup? "

        public static bool CanIPerformABackup(ServerConnection cnn, ILog logger, ref string errMessage)
        {
            bool retValue = true;
            Database db = null;
            Server server = null;
            bool isLogging = logger == null ? false : true;
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
                double dataBaseSize = db.Size;
                double freeToUse = 0;
                string driveLetterToCheck = server.BackupDirectory.Substring(0, 1);

                string command = @"DECLARE	@freeSpace float
                                   DECLARE @errorMessage nvarchar(1000)
                                   EXEC [IOHELPER].[FreeSpace] @driveLetter = N'{0}', @freeSpace = @freeSpace OUTPUT, @errorMessage = @errorMessage OUTPUT;
                                   SELECT @freeSpace AS N'@freeSpace', @errorMessage AS N'@errorMessage'";

                DataSet ds = db.ExecuteWithResults(String.Format(command, driveLetterToCheck));

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    freeToUse = (double)ds.Tables[0].Rows[0]["@freeSpace"];


                if ((freeToUse > dataBaseSize))
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log("Free on disk " + driveLetterToCheck + " in MB is : " + freeToUse.ToString() + " , and the expected backup size is  " + dataBaseSize.ToString() + " in MB");
                        logger.Log("..............................................................................................");
                    }
                    retValue = true;
                }
                else
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log("There is no enough space on drive " + driveLetterToCheck + " in MB is : " + freeToUse.ToString() + " , and the expected backup size is  " + dataBaseSize.ToString() + " in MB");
                        logger.Log("..............................................................................................");
                    }
                    retValue = false;
                }

            }
            catch (Exception ex)
            {
                retValue = false;
                errMessage = ex.Message;
                if (ex.InnerException != null)
                    errMessage += "\r\n" + ex.InnerException;
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("An error occures while trying to determine free disk space. The error message :" + errMessage);
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
                server = null;
                db = null;

            }

            return retValue;
        }
        #endregion



        #region " Delete old backup files "

        public static bool DeleteBackupFiles(ServerConnection cnn, ILog logger, ref string errMessage)
        {
            bool retValue = true;
            Database db = null;
            Server server = null;
            errMessage = string.Empty;
            bool isLogging = logger == null ? false : true;
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
                string dataBaseName = cnn.DatabaseName;
                                
                string filter = "dbHelper_" + dataBaseName + "_*.BAK";
                string backupDirectory = server.BackupDirectory;
                if (server.BackupDirectory.EndsWith(@"\") == false)
                    backupDirectory += @"\";


                string command = @"DECLARE @errorMessage nvarchar(1000)
                                   EXEC [IOHELPER].[DeleteFiles] @path = N'{0}', @filter = N'{1}', @errorMessage = @errorMessage OUTPUT;
                                   SELECT @errorMessage AS N'@errorMessage'";

                DataSet ds = db.ExecuteWithResults(String.Format(command, backupDirectory,filter));

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].Rows[0]["@errorMessage"].ToString().Trim() != string.Empty)
                    errMessage = (string)ds.Tables[0].Rows[0]["@errorMessage"];


                if (errMessage.Trim() == string.Empty)
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log("I have been deleted in directory : " + backupDirectory + " files with following pattern " + filter);
                        logger.Log("..............................................................................................");
                    }
                    if (db.ExtendedProperties[LAST_BACKUP] != null)
                        db.ExtendedProperties[LAST_BACKUP].Drop();

                    retValue = true;
                }
                else
                {
                    if (isLogging)
                    {
                        logger.Log("..............................................................................................");
                        logger.Log(@"There is an error when trying to delete old backup files in directory : " 
                                    + backupDirectory + " files with following patther " + filter + "Error description : " + errMessage);
                        logger.Log("..............................................................................................");
                    }
                    retValue = false;
                }

            }
            catch (Exception ex)
            {
                retValue = false;
                errMessage = ex.Message;
                if (ex.InnerException != null)
                    errMessage += "\r\n" + ex.InnerException;
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("An error occures while trying to delete old backup files. The error message :" + errMessage);
                    logger.Log("..............................................................................................");
                }

            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
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
                                    ref Int64 totalSizeInBytes,
                                    ref string errorMessage)
        {
            bool backupExists = true;
            Database db = null;
            Server server = null;
            bool checkIfExist = false;
            bool isLogging = logger == null ? false : true;
            ExtendedProperty extProperty = null;
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
                string dataBaseName = cnn.DatabaseName;
                string lastValidBackup = string.Empty;


                if (db.ExtendedProperties[LAST_BACKUP] != null)
                {
                    checkIfExist = true;
                    extProperty = db.ExtendedProperties[LAST_BACKUP];
                    lastValidBackup = extProperty.Value.ToString();
                }


                string filter = "dbHelper_" + dataBaseName + "_*.BAK";
                string backupDirectory = server.BackupDirectory;
                if (server.BackupDirectory.EndsWith(@"\") == false)
                    backupDirectory += @"\";

                string command = @"SELECT * FROM [IOHELPER].[FileHelper] (N'{0}',N'{1}')";

                DataSet ds = db.ExecuteWithResults(String.Format(command, backupDirectory, filter));
                if (ds.Tables.Count > 0)
                {
                    numberOfFiles = ds.Tables[0].Rows.Count;
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        if (checkIfExist && r["Name"].ToString().Equals(lastValidBackup))
                            backupExists = true;

                        totalSizeInBytes += (Int64)r["SizeInBytes"];
                    }
                }

            }
            catch ( Exception ex)
            {
                backupExists = false;
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\r\n" + ex.InnerException;
                if (isLogging)
                {
                    logger.Log("..............................................................................................");
                    logger.Log("An error occures while trying to determine how many backup file are taken. The error message :" + errorMessage);
                    logger.Log("..............................................................................................");
                }
            }
            finally
            {
                if (cnn != null && cnn.IsOpen)
                {
                    cnn.Disconnect();
                }
                server = null;
                db = null;
            }
            


            return backupExists;
        }

        #endregion

    }


}
