using Spectre.Console;
using System.Collections;
using System.Reflection;

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
        if (result == null)
        {
            AnsiConsole.MarkupLine("[italic grey]null[/]");
            return;
        }

        if (result is IEnumerable collection && result is not string)
        {
            var items = collection.Cast<object>().ToList();
            if (!items.Any())
            {
                AnsiConsole.MarkupLine("[italic]Empty collection[/]");
                return;
            }

            var table = new Table().Border(TableBorder.Rounded);
            var firstItem = items.First();
            var firstType = firstItem.GetType();

            var properties = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.CanRead)
                                      .ToArray();

            if (properties.Length == 0)
            {
                // used for maybe non-complex types like List<int>
                table.AddColumn("Value");
                foreach (var item in items.Take(20))
                {
                    table.AddRow(Markup.Escape(item?.ToString() ?? ""));
                }
            }
            else
            {
                foreach (var prop in properties)
                    table.AddColumn(prop.Name);

                foreach (var item in items.Take(20))
                {
                    var values = properties.Select(p =>
                    {
                        try
                        {
                            var val = p.GetValue(item);
                            return Markup.Escape(val?.ToString() ?? "<null>");
                        }
                        catch
                        {
                            return "[red]Error[/]";
                        }
                    }).ToArray();
                    table.AddRow(values);
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[grey]Total Items: {items.Count} (Showed {Math.Min(items.Count, 20)}) - {elapsedMs}ms[/]");
        }
        else
        {
            AnsiConsole.Write(new Panel(Markup.Escape(result.ToString() ?? ""))
                .Header($"Result - {elapsedMs}ms")
                .Expand()
                .BorderColor(Color.Green));
        }
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
