using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace SmoIntroduction;

/// <summary>The settings in the "Examples" section of appsettings.json, user secrets or DBHELPER_ environment variables.</summary>
public sealed class ExampleSettings
{
    /// <summary>The schema of CreateTable's table.</summary>
    public string TestSchema { get; set; } = "HumanResources";

    /// <summary>CreateTable's table.</summary>
    public string TestTable { get; set; } = "TestTable";

    /// <summary>The schema of CreateMoTable's memory-optimized table.</summary>
    public string MoTestSchema { get; set; } = "MOHumanResources";

    /// <summary>CreateMoTable's memory-optimized table.</summary>
    public string MoTestTable { get; set; } = "MOTestTable";

    /// <summary>
    /// Where CreateMoTable puts the container of the memory-optimized filegroup, on the server;
    /// empty for a folder next to master's files.
    /// </summary>
    public string MoContainerPath { get; set; } = "";

    /// <summary>The database the DbHelper tour backs up and restores. The tour creates it when it is missing.</summary>
    public string TourDatabase { get; set; } = "DbHelperDemo";

    /// <summary>
    /// Protects master's database master key, when an example has to create it, and the private keys
    /// the examples export. Keep it in user secrets.
    /// </summary>
    public string KeyPassword { get; set; } = "";
}

/// <summary>What an example needs: the connection string and the settings.</summary>
public sealed class ExampleContext(string connectionString, ExampleSettings settings)
{
    public string ConnectionString { get; } = connectionString;

    public ExampleSettings Settings { get; } = settings;

    /// <summary>The database in the connection string, which most examples work in.</summary>
    public string DatabaseName => new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;

    /// <summary>A new connection for SMO to the database in the connection string.</summary>
    public ServerConnection Connect() => new(new SqlConnection(ConnectionString));

    /// <summary>A new connection for SMO to another database on the same server.</summary>
    public ServerConnection Connect(string database) =>
        new(new SqlConnection(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = database }.ToString()));

    /// <summary>The key password, which an example needs only when it creates keys.</summary>
    public string RequireKeyPassword() =>
        Settings.KeyPassword.Length > 0
            ? Settings.KeyPassword
            : throw new InvalidOperationException(
                "Set a password for the keys first: dotnet user-secrets set \"Examples:KeyPassword\" \"<a strong password>\" --project SmoIntroduction");
}

/// <summary>One of the examples the menu offers.</summary>
public interface IExample
{
    /// <summary>What the example shows.</summary>
    string Description { get; }

    void Run(ExampleContext context);
}
