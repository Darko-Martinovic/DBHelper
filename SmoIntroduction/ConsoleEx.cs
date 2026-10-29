namespace SmoIntroduction;

public static class ConsoleEx
{
    public static void WriteLine(string message, ConsoleColor cc = ConsoleColor.White,
        ConsoleColor bc = ConsoleColor.Black)
    {
        Console.ForegroundColor = cc;
        Console.BackgroundColor = bc;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>Shows a T-SQL script SMO made, where the examples used to open it in Notepad.</summary>
    public static void WriteScript(IEnumerable<string?> batches)
    {
        Console.WriteLine(new string('-', 79));
        foreach (var batch in batches)
            WriteLine(batch ?? "", ConsoleColor.Gray);
        Console.WriteLine(new string('-', 79));
    }
}
