using Microsoft.Extensions.Configuration;
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

    public static string AskForDllPath(IConfiguration config)
    {
        var envPath = config["TinkNet:DllPath"] ?? config["TINKNET_DLL_PATH"];
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            AnsiConsole.MarkupLine($"[grey]Loaded DLL path from config: {envPath}[/]");
            return envPath.Trim('"');
        }

        var dllPath = AnsiConsole.Ask<string>("Path to DLL [bold blue]>[/]");
        return dllPath.Trim('"');
    }

    public static string? AskToConfigureDatabase(Type dbContextType, IConfiguration config)
    {
        AnsiConsole.Write(new Panel(dbContextType.FullName ?? dbContextType.Name)
            .Header("DbContext Found")
            .BorderColor(Color.Green));

        var envProvider = config["TinkNet:DbProvider"] ?? config["TINKNET_DB_PROVIDER"];
        var envConnectionString = config["TinkNet:ConnectionString"] ?? config["TINKNET_CONNECTION_STRING"];

        DbProvider provider = DbProvider.SqlServer; // Provide a default
        bool hasValidEnvProvider = !string.IsNullOrWhiteSpace(envProvider) && Enum.TryParse(envProvider, true, out provider);
        bool hasEnvConnectionString = !string.IsNullOrWhiteSpace(envConnectionString);

        if (!hasValidEnvProvider || !hasEnvConnectionString)
        {
            if (!AnsiConsole.Confirm("Do you want to configure the database connection now?"))
            {
                return null;
            }
        }

        if (hasValidEnvProvider)
        {
            AnsiConsole.MarkupLine($"[grey]Loaded database provider from config: {provider}[/]");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(envProvider))
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Invalid configured DbProvider '{envProvider}'. Falling back to prompt.[/]");
            }

            provider = AnsiConsole.Prompt(
                new SelectionPrompt<DbProvider>()
                    .Title("Select Database Provider")
                    .AddChoices(Enum.GetValues<DbProvider>()));
        }

        string connectionString;
        if (hasEnvConnectionString)
        {
            AnsiConsole.MarkupLine($"[grey]Loaded connection string from config.[/]");
            connectionString = envConnectionString!;
        }
        else
        {
            connectionString = AnsiConsole.Ask<string>("Enter Connection String:");
        }
        
        // Escape quotes
        connectionString = connectionString.Replace("\"", "\\\"");

        var dbContextName = dbContextType.FullName ?? dbContextType.Name;

        AnsiConsole.MarkupLine($"[grey]Initializing context with {provider}...[/]");
        AnsiConsole.MarkupLine($"[grey]You can access your application's {dbContextName} with 'db'...[/]");
        AnsiConsole.MarkupLine($"[grey]Tip: Type 'ShowSql = false;' anytime to stop printing translated SQL queries.[/]");
        
        // Return the script code to initialize the db variable
        return $"var db = GetContext<{dbContextName}>(\"{connectionString}\", DbProvider.{provider});";
    }

    public static void ShowDbHelp(Type dbContextType)
    {
        AnsiConsole.MarkupLine($"[grey]Tip: Use helper: var db = GetContext<{dbContextType.Name}>(\"conn_string\", DbProvider.SqlServer);[/]");
        AnsiConsole.MarkupLine($"[grey]Tip: Type 'ShowSql = false;' anytime to stop printing translated SQL queries.[/]");
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
