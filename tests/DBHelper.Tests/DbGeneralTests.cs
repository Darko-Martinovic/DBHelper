using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;

namespace DBHelper.Tests;

public sealed class DbGeneralTests(SqlServer sql)
{
    private const string BackupDirectory = "/var/opt/mssql/data";

    [Fact]
    public async Task BackupDatabase_writes_a_file_that_the_backup_history_and_the_file_list_know()
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);

        var file = DbGeneral.BackupDatabase(cnn);

        Assert.StartsWith($"{BackupDirectory}/dbHelper_{db}_", file);
        Assert.EndsWith(".bak", file);
        Assert.Equal(file, DbGeneral.LastBackupFile(cnn));
        Assert.Equal([file], DbGeneral.DetermineNumberOfBackupFiles(cnn).Paths);
        Assert.True(DbGeneral.IsThereAnyBackupTaken(cnn));
        Assert.False(cnn.IsOpen);
    }

    [Fact]
    public async Task There_is_no_backup_of_a_new_database()
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);

        Assert.Null(DbGeneral.LastBackupFile(cnn));
        Assert.False(DbGeneral.IsThereAnyBackupTaken(cnn));
        Assert.Equal(0, DbGeneral.DetermineNumberOfBackupFiles(cnn).Count);
        Assert.Throws<InvalidOperationException>(() => DbGeneral.RestoreDatabase(cnn));
    }

    [Fact]
    public async Task RestoreDatabase_brings_back_the_last_backup()
    {
        var db = await sql.CreateDatabaseAsync();
        await sql.ExecuteAsync(db, "CREATE TABLE dbo.T (Id int); INSERT dbo.T VALUES (1);");
        var cnn = sql.Connect(db);
        var file = DbGeneral.BackupDatabase(cnn);
        await sql.ExecuteAsync(db, "INSERT dbo.T VALUES (2);");

        Assert.Equal(file, DbGeneral.RestoreDatabase(cnn));

        Assert.Equal(1, await sql.ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.T;"));
        Assert.Equal("MULTI_USER", await sql.ScalarAsync<string>("master", $"SELECT user_access_desc FROM sys.databases WHERE name = N'{db}';"));
    }

    [Fact]
    public async Task The_methods_share_a_connection_that_is_already_open()
    {
        var db = await sql.CreateDatabaseAsync();
        await sql.ExecuteAsync(db, "CREATE TABLE dbo.T (Id int);");
        var cnn = sql.Connect(db);
        cnn.Connect();

        var file = DbGeneral.BackupDatabase(cnn);
        DbGeneral.RestoreDatabase(cnn);

        Assert.True(cnn.IsOpen);
        Assert.Equal(file, DbGeneral.LastBackupFile(cnn));
        Assert.True(DbGeneral.IsTheDataBaseOnLine(cnn));
        Assert.Empty(DbGeneral.CheckDb(cnn));
        cnn.Disconnect();
    }

    [Fact]
    public async Task DeleteBackupFiles_keeps_the_newest()
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);
        DbGeneral.BackupDatabase(cnn, verify: false);
        DbGeneral.BackupDatabase(cnn, verify: false);
        var newest = DbGeneral.BackupDatabase(cnn, verify: false);
        Assert.Equal(3, DbGeneral.DetermineNumberOfBackupFiles(cnn).Count);

        Assert.Equal(2, DbGeneral.DeleteBackupFiles(cnn));

        var left = DbGeneral.DetermineNumberOfBackupFiles(cnn);
        Assert.Equal([newest], left.Paths);
        Assert.True(left.TotalSizeInBytes > 0);
        Assert.True(DbGeneral.IsThereAnyBackupTaken(cnn));

        Assert.Equal(1, DbGeneral.DeleteBackupFiles(cnn, keepLatest: 0));
        Assert.False(DbGeneral.IsThereAnyBackupTaken(cnn));
    }

    [Fact]
    public async Task DeleteBackupFiles_leaves_other_databases_backups_alone()
    {
        var db = await sql.CreateDatabaseAsync();
        var other = await sql.CreateDatabaseAsync();
        var otherBackup = DbGeneral.BackupDatabase(sql.Connect(other), verify: false);

        DbGeneral.BackupDatabase(sql.Connect(db), verify: false);
        DbGeneral.DeleteBackupFiles(sql.Connect(db), keepLatest: 0);

        Assert.Equal([otherBackup], DbGeneral.DetermineNumberOfBackupFiles(sql.Connect(other)).Paths);
    }

    [Fact]
    public async Task CanIPerformABackup_finds_the_free_space_of_the_backup_drive()
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);

        Assert.True(DbGeneral.CanIPerformABackup(cnn, DiskSizeUnit.GigaBytes));
        var freeMb = DbGeneral.DetermineFreeSpaceForBackups(cnn);
        Assert.True(freeMb > 0);
        Assert.Equal(freeMb / 1024, DbGeneral.DetermineFreeSpaceForBackups(cnn, DiskSizeUnit.GigaBytes), 3);
    }

    [Theory]
    [InlineData(DiskSizeUnit.Bytes, 2 * 1024 * 1024)]
    [InlineData(DiskSizeUnit.KiloBytes, 2 * 1024)]
    [InlineData(DiskSizeUnit.MegaBytes, 2)]
    [InlineData(DiskSizeUnit.GigaBytes, 2.0 / 1024)]
    [InlineData(DiskSizeUnit.TeraBytes, 2.0 / 1024 / 1024)]
    public void DetermineSize_converts_megabytes(DiskSizeUnit unit, double expected) =>
        Assert.Equal(expected, DbGeneral.DetermineSize(2, unit), 12);

    [Fact]
    public async Task CheckDb_reports_nothing_on_a_healthy_database_and_leaves_it_multi_user()
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);

        Assert.Empty(DbGeneral.CheckDb(cnn));
        Assert.Empty(DbGeneral.CheckDb(cnn, repairAllowDataLoss: true));
        Assert.Equal("MULTI_USER", await sql.ScalarAsync<string>("master", $"SELECT user_access_desc FROM sys.databases WHERE name = N'{db}';"));
    }

    [Fact]
    public async Task PutDbInUserMode_changes_the_user_access()
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);

        DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Restricted, true);
        Assert.Equal("RESTRICTED_USER", await sql.ScalarAsync<string>("master", $"SELECT user_access_desc FROM sys.databases WHERE name = N'{db}';"));

        DbGeneral.PutDbInUserMode(cnn, DatabaseUserAccess.Multiple, true);
        Assert.Equal("MULTI_USER", await sql.ScalarAsync<string>("master", $"SELECT user_access_desc FROM sys.databases WHERE name = N'{db}';"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetRecoveryMode_changes_the_recovery_model(bool killUserProcess)
    {
        var db = await sql.CreateDatabaseAsync();
        var cnn = sql.Connect(db);

        DbGeneral.SetRecoveryMode(cnn, RecoveryModel.Simple, killUserProcess);
        Assert.Equal("SIMPLE", await sql.ScalarAsync<string>("master", $"SELECT recovery_model_desc FROM sys.databases WHERE name = N'{db}';"));

        DbGeneral.SetRecoveryMode(cnn, RecoveryModel.Full, killUserProcess);
        Assert.Equal("FULL", await sql.ScalarAsync<string>("master", $"SELECT recovery_model_desc FROM sys.databases WHERE name = N'{db}';"));
    }

    [Fact]
    public async Task KillAllProcessesForOneDatabase_ends_the_other_sessions()
    {
        var db = await sql.CreateDatabaseAsync();
        await using var other = new SqlConnection(sql.ConnectionString(db) + ";Pooling=false;ConnectRetryCount=0");
        await other.OpenAsync(TestContext.Current.CancellationToken);

        DbGeneral.KillAllProcessesForOneDatabase(sql.Connect(db));

        await using var command = new SqlCommand("SELECT 1;", other);
        await Assert.ThrowsAnyAsync<SqlException>(() => command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ForceShrinkingLog_shrinks_the_log_and_keeps_the_log_chain()
    {
        var db = await sql.CreateDatabaseAsync();
        await sql.ExecuteAsync("master", $"ALTER DATABASE [{db}] SET RECOVERY FULL; ALTER DATABASE [{db}] MODIFY FILE (NAME = N'{db}_log', SIZE = 64MB);");
        var cnn = sql.Connect(db);
        DbGeneral.BackupDatabase(cnn, copyOnly: false, verify: false);
        Assert.True(DbGeneral.DetermineLogSize(cnn) >= 64);

        var result = DbGeneral.ForceShrinkingLog(cnn, 1);

        Assert.True(result.BeforeMb >= 64);
        Assert.True(result.AfterMb < 64);
        Assert.Equal(result.AfterMb, DbGeneral.DetermineLogSize(cnn));
        Assert.Equal("FULL", await sql.ScalarAsync<string>("master", $"SELECT recovery_model_desc FROM sys.databases WHERE name = N'{db}';"));
        // A log backup needs a full backup after the switch back to the full recovery model
        await sql.ExecuteAsync("master", $"BACKUP LOG [{db}] TO DISK = N'{BackupDirectory}/{db}.trn' WITH INIT;");
    }

    [Fact]
    public async Task IsTheDataBaseOnLine_is_false_for_an_offline_or_missing_database()
    {
        var db = await sql.CreateDatabaseAsync();
        Assert.True(DbGeneral.IsTheDataBaseOnLine(sql.Connect(db)));

        await sql.ExecuteAsync("master", $"ALTER DATABASE [{db}] SET OFFLINE WITH ROLLBACK IMMEDIATE;");
        Assert.False(DbGeneral.IsTheDataBaseOnLine(sql.Connect(db)));
        await sql.ExecuteAsync("master", $"ALTER DATABASE [{db}] SET ONLINE;");

        Assert.False(DbGeneral.IsTheDataBaseOnLine(sql.Connect("no_such_database")));
    }

    [Fact]
    public async Task An_encrypted_backup_needs_its_certificate_and_restores()
    {
        var db = await sql.CreateDatabaseAsync();
        await sql.ExecuteAsync(db, "CREATE TABLE dbo.T (Id int); INSERT dbo.T VALUES (1);");
        var cnn = sql.Connect(db);
        var certificate = $"cert_{db}";
        var encryption = new BackupEncryptionOptions
        {
            NoEncryption = false,
            Algorithm = BackupEncryptionAlgorithm.Aes256,
            EncryptorType = BackupEncryptorType.ServerCertificate,
            EncryptorName = certificate
        };

        Assert.Throws<InvalidOperationException>(() => DbGeneral.BackupDatabase(cnn, encryption));
        Assert.False(DbGeneral.IsThereAnyBackupTaken(cnn));

        DbGeneral.CreateBackupCertificate(cnn, "Master!Key!Passw0rd", certificate,
            $"{BackupDirectory}/{certificate}.cer", $"{BackupDirectory}/{certificate}.pvk", "Private!Key!Passw0rd");
        Assert.True(DbGeneral.BackupCertificateExists(cnn, certificate));
        Assert.True(DbGeneral.MasterKeyExists(cnn));

        var file = DbGeneral.BackupDatabase(cnn, encryption);
        Assert.Equal(certificate, await sql.ScalarAsync<string>("msdb",
            $"SELECT TOP (1) c.name FROM dbo.backupset AS bs INNER JOIN master.sys.certificates AS c ON c.thumbprint = bs.encryptor_thumbprint WHERE bs.database_name = N'{db}' ORDER BY bs.backup_set_id DESC;"));
        await sql.ExecuteAsync(db, "INSERT dbo.T VALUES (2);");
        Assert.Equal(file, DbGeneral.RestoreDatabase(cnn));
        Assert.Equal(1, await sql.ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.T;"));
    }

    [Fact]
    public async Task CreateBackupCertificate_keeps_the_master_key_and_an_existing_certificate()
    {
        await sql.ExecuteAsync("master",
            "IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##') CREATE MASTER KEY ENCRYPTION BY PASSWORD = N'Master!Key!Passw0rd';");
        var masterKey = await sql.ScalarAsync<Guid>("master", "SELECT key_guid FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##';");
        var cnn = sql.Connect("master");
        var certificate = $"cert_{Guid.NewGuid():N}";

        DbGeneral.CreateBackupCertificate(cnn, "Another!Passw0rd", certificate);
        var thumbprint = await sql.ScalarAsync<byte[]>("master", $"SELECT thumbprint FROM sys.certificates WHERE name = N'{certificate}';");

        Assert.Throws<InvalidOperationException>(() => DbGeneral.CreateBackupCertificate(cnn, "Another!Passw0rd", certificate));
        Assert.Equal(masterKey, await sql.ScalarAsync<Guid>("master", "SELECT key_guid FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##';"));
        Assert.Equal(thumbprint, await sql.ScalarAsync<byte[]>("master", $"SELECT thumbprint FROM sys.certificates WHERE name = N'{certificate}';"));
    }
}
