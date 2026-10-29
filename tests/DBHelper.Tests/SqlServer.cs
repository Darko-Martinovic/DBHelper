using DotNet.Testcontainers.Configurations;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(DBHelper.Tests.SqlServer))]

namespace DBHelper.Tests;

/// <summary>
/// SQL Server in a container, for all the tests: 2025 unless MSSQL_IMAGE names another image, as CI does for 2022.
/// The examples' tests restore AdventureWorks into it, the first time one asks.
/// </summary>
public sealed class SqlServer : IAsyncLifetime
{
    private static readonly string Image =
        Environment.GetEnvironmentVariable("MSSQL_IMAGE") ?? "mcr.microsoft.com/mssql/server:2025-latest";

    // AdventureWorks2017 restores on 2017 and later, and is a quarter of the size of 2019's and 2022's
    private const string AdventureWorksUrl =
        "https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2017.bak";

    public const string AdventureWorks = "AdventureWorks";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();
    private readonly SemaphoreSlim _adventureWorksLock = new(1, 1);
    private bool _adventureWorksRestored;

    public async ValueTask InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception e)
        {
            // Show why SQL Server didn't start, not only that it didn't
            var (stdout, stderr) = await _container.GetLogsAsync();
            throw new InvalidOperationException($"SQL Server didn't start:\n{stdout}\n{stderr}", e);
        }
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public string ConnectionString(string database) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = database }.ToString();

    /// <summary>A connection for SMO and DbGeneral, to a database.</summary>
    public ServerConnection Connect(string database) => new(new SqlConnection(ConnectionString(database)));

    /// <summary>A new, empty database with a name of its own.</summary>
    public async Task<string> CreateDatabaseAsync(string prefix = "dbh")
    {
        var name = $"{prefix}_{Guid.NewGuid():N}"[..20];
        await ExecuteAsync("master", $"CREATE DATABASE [{name}];");
        return name;
    }

    public async Task ExecuteAsync(string database, string sql)
    {
        await using var connection = new SqlConnection(ConnectionString(database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string database, string sql)
    {
        await using var connection = new SqlConnection(ConnectionString(database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        return (T)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Restores AdventureWorks as <see cref="AdventureWorks"/>, once: the examples are written against it.</summary>
    public async Task RestoreAdventureWorksAsync()
    {
        await _adventureWorksLock.WaitAsync();
        try
        {
            if (_adventureWorksRestored)
                return;

            var backup = await DownloadAdventureWorksAsync();
            await _container.CopyAsync(await File.ReadAllBytesAsync(backup), "/var/opt/mssql/backup/AdventureWorks2017.bak",
                fileMode: Unix.FileMode644);
            await ExecuteAsync("master",
                $"""
                 RESTORE DATABASE [{AdventureWorks}] FROM DISK = N'/var/opt/mssql/backup/AdventureWorks2017.bak'
                 WITH MOVE N'AdventureWorks2017' TO N'/var/opt/mssql/data/{AdventureWorks}.mdf',
                      MOVE N'AdventureWorks2017_log' TO N'/var/opt/mssql/data/{AdventureWorks}_log.ldf',
                      REPLACE;
                 """);
            _adventureWorksRestored = true;
        }
        finally
        {
            _adventureWorksLock.Release();
        }
    }

    // Kept between runs in ADVENTUREWORKS_CACHE, or the temp folder, as CI caches it
    private static async Task<string> DownloadAdventureWorksAsync()
    {
        var folder = Environment.GetEnvironmentVariable("ADVENTUREWORKS_CACHE") ?? Path.Combine(Path.GetTempPath(), "dbhelper-tests");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "AdventureWorks2017.bak");
        if (File.Exists(file))
            return file;

        using var http = new HttpClient();
        var partial = file + ".part";
        await using (var target = File.Create(partial))
        await using (var source = await http.GetStreamAsync(AdventureWorksUrl))
        {
            await source.CopyToAsync(target);
        }

        File.Move(partial, file, overwrite: true);
        return file;
    }
}
