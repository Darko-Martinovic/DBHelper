using DBHelper;
using Microsoft.Extensions.Configuration;
using SmoIntroduction;
using Spectre.Console;

// The settings: appsettings.json, then user secrets, then DBHELPER_ environment variables
// (DBHELPER_ConnectionStrings__ConnStr, DBHELPER_Examples__KeyPassword)
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<ExampleContext>(optional: true)
    .AddEnvironmentVariables("DBHELPER_")
    .Build();

var connectionString = configuration.GetConnectionString("ConnStr");
if (string.IsNullOrWhiteSpace(connectionString))
{
    AnsiConsole.MarkupLine("[red]Set ConnectionStrings:ConnStr in appsettings.json or user secrets.[/]");
    return 1;
}

var context = new ExampleContext(connectionString, configuration.GetSection("Examples").Get<ExampleSettings>() ?? new ExampleSettings());

IReadOnlyList<IExample> examples =
[
    new CreateGraph(),
    new CreateMoTable(),
    new CreatePartitionScheme(),
    new CreateStoredProc(),
    new CreateSysVerTempTables(),
    new CreateTable(),
    new ExtProperties(),
    new ExtendedEvents(),
    new ListAllUserDefinedDataTypes(),
    new RebuildIndexes(),
    new ServerConfiguration(),
    new ServerEvents(),
    new SetDefaultInitField(),
    new Tde(),
    new DbHelperTour()
];

// One example by its name, such as "dotnet run --project SmoIntroduction -- CreateGraph", or the menu
if (args.Length > 0)
{
    var example = examples.FirstOrDefault(e => string.Equals(e.GetType().Name, args[0], StringComparison.OrdinalIgnoreCase));
    if (example == null)
    {
        AnsiConsole.MarkupLineInterpolated($"[red]There is no example {args[0]}. The examples: {string.Join(", ", examples.Select(e => e.GetType().Name))}[/]");
        return 1;
    }

    return Run(example) ? 0 : 1;
}

while (true)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new Rule($"[yellow]SMO introduction[/] on {Markup.Escape(context.DatabaseName)}"));
    const int exit = -1;
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<int>()
            .Title("Which example?")
            .PageSize(20)
            .AddChoices(Enumerable.Range(0, examples.Count))
            .AddChoices(exit)
            .UseConverter(i => i == exit
                ? "Exit"
                : $"{examples[i].GetType().Name} [grey]{Markup.Escape(examples[i].Description)}[/]"));
    if (choice == exit)
        return 0;

    Run(examples[choice]);
    AnsiConsole.MarkupLine("[grey]Press any key to return to the menu...[/]");
    Console.ReadKey(intercept: true);
}

bool Run(IExample example)
{
    AnsiConsole.Write(new Rule(example.GetType().Name));
    try
    {
        example.Run(context);
        return true;
    }
    catch (Exception ex)
    {
        // The innermost exceptions carry SQL Server's message
        ConsoleEx.WriteLine(ex.AllMessages(), ConsoleColor.Red);
        return false;
    }
}
