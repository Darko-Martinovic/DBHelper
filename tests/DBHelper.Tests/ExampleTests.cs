using SmoIntroduction;

namespace DBHelper.Tests;

/// <summary>The examples, run against AdventureWorks as a reader of the article would run them.</summary>
public sealed class ExampleTests(SqlServer sql) : IAsyncLifetime
{
    private const string Db = SqlServer.AdventureWorks;

    public async ValueTask InitializeAsync() => await sql.RestoreAdventureWorksAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ExampleContext Context(string? tourDatabase = null) =>
        new(sql.ConnectionString(Db), new ExampleSettings
        {
            KeyPassword = "Example!Key!Passw0rd",
            TourDatabase = tourDatabase ?? "DbHelperDemo"
        });

    // What the example wrote to the console
    private string Run(IExample example, ExampleContext? context = null)
    {
        var original = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            example.Run(context ?? Context());
        }
        finally
        {
            Console.SetOut(original);
        }

        return output.ToString();
    }

    [Fact]
    public async Task CreateGraph_finds_the_restaurants_John_likes()
    {
        var output = Run(new CreateGraph());

        Assert.Equal(5, await sql.ScalarAsync<int>(Db, "SELECT COUNT(*) FROM dbo.Person;"));
        Assert.Equal("Taco Dell", await sql.ScalarAsync<string>(Db,
            "SELECT Restaurant.name FROM Person, likes, Restaurant WHERE MATCH (Person-(likes)->Restaurant) AND Person.name = 'John';"));
        // What John's friend Mary likes, and Jacob, who likes a restaurant in Redmond, where he lives
        Assert.Contains("Ginger and Spice", output);
        Assert.Contains("Jacob", output);
        Assert.DoesNotContain("Julie", output);
    }

    [Fact]
    public async Task CreateMoTable_creates_a_memory_optimized_table_and_runs_again()
    {
        Run(new CreateMoTable());
        var output = Run(new CreateMoTable());

        Assert.True(await sql.ScalarAsync<bool>(Db,
            "SELECT is_memory_optimized FROM sys.tables WHERE object_id = OBJECT_ID(N'MOHumanResources.MOTestTable');"));
        Assert.Contains("MEMORY_OPTIMIZED = ON", output);
    }

    [Fact]
    public async Task CreatePartitionScheme_spreads_the_rows_over_four_partitions()
    {
        Run(new CreatePartitionScheme());

        Assert.Equal("1:2,2:1,3:1,4:1", await sql.ScalarAsync<string>(CreatePartitionScheme.DataBaseName,
            """
            SELECT STRING_AGG(CONCAT(partition_number, ':', row_count), ',') WITHIN GROUP (ORDER BY partition_number)
            FROM sys.dm_db_partition_stats WHERE object_id = OBJECT_ID(N'dbo.TestTable');
            """));
    }

    [Fact]
    public async Task CreateStoredProc_creates_an_encrypted_procedure()
    {
        var output = Run(new CreateStoredProc());

        Assert.Equal(1, await sql.ScalarAsync<int>(Db,
            "SELECT COUNT(*) FROM sys.sql_modules WHERE object_id = OBJECT_ID(N'dbo.SmoTest') AND definition IS NULL;"));
        Assert.Contains("CREATE PROCEDURE", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSysVerTempTables_makes_the_table_temporal_and_runs_again()
    {
        Run(new CreateSysVerTempTables());
        var output = Run(new CreateSysVerTempTables());

        Assert.Equal("SYSTEM_VERSIONED_TEMPORAL_TABLE", await sql.ScalarAsync<string>(Db,
            "SELECT temporal_type_desc FROM sys.tables WHERE object_id = OBJECT_ID(N'Person.EmailAddress');"));
        Assert.Equal("test0@adventure-works.com", await sql.ScalarAsync<string>(Db,
            "SELECT EmailAddress FROM Person.EmailAddress WHERE BusinessEntityID = 1 AND EmailAddressID = 1;"));
        // As of midnight, the address was the one AdventureWorks came with
        Assert.Contains("The Email Address at midnight (UTC) was ken0@adventure-works.com", output);
    }

    [Fact]
    public async Task CreateTable_creates_the_table_and_scripts_it()
    {
        var output = Run(new CreateTable());

        Assert.Equal(1, await sql.ScalarAsync<int>(Db, "SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'HumanResources.TestTable');"));
        Assert.Contains("CREATE TABLE [HumanResources].[TestTable]", output);
    }

    [Fact]
    public async Task ExtProperties_sets_the_creator_on_eleven_objects_and_runs_again()
    {
        Run(new ExtProperties());
        Run(new ExtProperties());

        Assert.Equal(11, await sql.ScalarAsync<int>(Db,
            $"SELECT COUNT(*) FROM sys.extended_properties WHERE name = N'{ExtProperties.Creator}' AND CAST(value AS nvarchar(100)) = N'{ExtProperties.Value}';"));
    }

    [Fact]
    public async Task ExtendedEvents_starts_a_session_with_a_file_target()
    {
        try
        {
            Run(new ExtendedEvents());
            Run(new ExtendedEvents());

            Assert.Equal(1, await sql.ScalarAsync<int>("master",
                $"SELECT COUNT(*) FROM sys.dm_xe_session_targets AS t INNER JOIN sys.dm_xe_sessions AS s ON s.address = t.event_session_address WHERE s.name = N'{ExtendedEvents.SessionName}' AND t.target_name = N'event_file';"));
        }
        finally
        {
            await sql.ExecuteAsync("master", $"IF EXISTS (SELECT * FROM sys.server_event_sessions WHERE name = N'{ExtendedEvents.SessionName}') DROP EVENT SESSION [{ExtendedEvents.SessionName}] ON SERVER;");
        }
    }

    [Fact]
    public void ListAllUserDefinedDataTypes_scripts_the_types()
    {
        var output = Run(new ListAllUserDefinedDataTypes());

        Assert.Contains("dbo.Flag", output);
        Assert.Contains("CREATE TYPE [dbo].[Flag]", output);
    }

    [Fact]
    public async Task RebuildIndexes_rebuilds_every_disk_based_table()
    {
        var output = Run(new RebuildIndexes());

        var tables = await sql.ScalarAsync<int>(Db, "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND is_memory_optimized = 0;");
        Assert.Equal(tables, output.Split('\n').Count(line => line.StartsWith("Updating statistics for table", StringComparison.Ordinal)));
    }

    [Fact]
    public void ServerConfiguration_lists_the_configuration()
    {
        var output = Run(new ServerConfiguration());

        Assert.Contains("max server memory (MB)", output);
    }

    [Fact]
    public void ServerEvents_explains_that_it_needs_Windows_on_both_sides()
    {
        // The container is SQL Server on Linux, which has no WMI to send events through
        var output = Run(new ServerEvents());

        Assert.Contains("WMI", output);
    }

    [Fact]
    public void SetDefaultInitField_lists_the_databases()
    {
        var output = Run(new SetDefaultInitField());

        Assert.Contains(Db, output);
        Assert.Contains("master", output);
    }

    [Fact]
    public async Task Tde_encrypts_the_database_and_runs_again()
    {
        Run(new Tde());
        var output = Run(new Tde());

        Assert.True(await sql.ScalarAsync<bool>("master", $"SELECT is_encrypted FROM sys.databases WHERE name = N'{Tde.DbName}';"));
        Assert.Equal(140, await sql.ScalarAsync<byte>("master", $"SELECT compatibility_level FROM sys.databases WHERE name = N'{Tde.DbName}';"));
        Assert.Contains("COMPATIBILITY_LEVEL = 140", output);
    }

    [Fact]
    public void DbHelperTour_runs_every_task()
    {
        var output = Run(new DbHelperTour(), Context($"tour_{Guid.NewGuid():N}"[..16]));

        Assert.DoesNotContain("failed", output);
        Assert.Equal(13, output.Split('\n').Count(line => line.StartsWith("The task finished successfully!", StringComparison.Ordinal)));
    }
}
