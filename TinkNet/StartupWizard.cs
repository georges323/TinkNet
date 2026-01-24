using Spectre.Console;

namespace TinkNet;

public static class StartupWizard
{
    public static void ShowHeader()
    {
        AnsiConsole.Write(new FigletText("TinkNet").Color(Color.Green));
    }

    public static string AskForDllPath()
    {
        var dllPath = AnsiConsole.Ask<string>("Path to DLL [bold blue]>[/]");
        return dllPath.Trim('"');
    }

    public static string? AskToConfigureDatabase(Type dbContextType)
    {
        AnsiConsole.Write(new Panel(dbContextType.FullName ?? dbContextType.Name)
            .Header("DbContext Found")
            .BorderColor(Color.Green));

        if (!AnsiConsole.Confirm("Do you want to configure the database connection now?"))
        {
            return null;
        }

        var provider = AnsiConsole.Prompt(
            new SelectionPrompt<DbProvider>()
                .Title("Select Database Provider")
                .AddChoices(Enum.GetValues<DbProvider>()));

        // GTODO: Find a good way to read the connection string based on appsettings.json if we run this in the 
        // target application's directory
        var connectionString = AnsiConsole.Ask<string>("Enter Connection String:");
        
        // Escape quotes
        connectionString = connectionString.Replace("\"", "\\\"");

        var dbContextName = dbContextType.FullName ?? dbContextType.Name;

        AnsiConsole.MarkupLine($"[grey]Initializing context with {provider}...[/]");
        AnsiConsole.MarkupLine($"[grey]You can access your application's {dbContextName} with 'db'...[/]");
        
        // Return the script code to initialize the db variable
        return $"var db = GetContext<{dbContextName}>(\"{connectionString}\", DbProvider.{provider});";
    }

    public static void ShowDbHelp(Type dbContextType)
    {
        AnsiConsole.MarkupLine($"[grey]Tip: Use helper: var db = GetContext<{dbContextType.Name}>(\"conn_string\", DbProvider.SqlServer);[/]");
    }

    public static string AskForCode()
    {
        return AnsiConsole.Ask<string>("[bold blue]>[/]");
    }

    public static void DisplayResult(object? result, long elapsedMs)
    {
        if (result == null) return;
        
        string resultText = result.ToString() ?? "null";
        
        AnsiConsole.Write(new Panel(resultText)
            .Header($"Result - {elapsedMs}ms")
            .Expand()
            .BorderColor(Color.Green));
    }

    public static void DisplayError(string title, string message)
    {
        AnsiConsole.MarkupLine($"[red]{title}[/]");
        AnsiConsole.MarkupLine($"  [red]{message}[/]");
    }

    public static void DisplayException(Exception ex)
    {
        AnsiConsole.WriteException(ex);
    }
}
