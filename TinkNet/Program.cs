using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Spectre.Console;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TinkNet;

AnsiConsole.Write(new FigletText("TinkNet").Color(Color.Green));

var dllPath = AnsiConsole.Ask<string>("Path to DLL [bold blue]>[/]");
// Support quoted paths
dllPath = dllPath.Trim('"');

Assembly targetAssembly;
try
{
    targetAssembly = Loader.Load(dllPath);
    AnsiConsole.MarkupLine($"[green]Successfully loaded assembly:[/] {targetAssembly.GetName().Name}");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Failed to load assembly:[/] {ex.Message}");
    return;
}

var options = ScriptOptions.Default
    .AddReferences(typeof(Enumerable).Assembly)
    .AddReferences(typeof(DbContext).Assembly) 
    .AddReferences(targetAssembly)
    .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Threading.Tasks", "Microsoft.EntityFrameworkCore");

var dependentReferences = Loader.GetAssemblyReferences();
if (dependentReferences.Any())
{
    options = options.AddReferences(dependentReferences);
}

var types = Loader.GetLoadableTypes(targetAssembly);
var dbContextType = types.FirstOrDefault(t => t.IsSubclassOf(typeof(DbContext)) && !t.IsAbstract);

if (dbContextType is null)
{
    AnsiConsole.MarkupLine("[red]No concrete DbContext found in this DLL![/]");
    // Continue anyway, maybe they just want to inspect other types
}
else
{
    AnsiConsole.Write(new Panel(dbContextType.FullName ?? dbContextType.Name)
        .Header("DbContext Found")
        .BorderColor(Color.Green));
}

if (dbContextType?.Namespace != null)
{
    options = options.AddImports(dbContextType.Namespace);
}

ScriptState<object>? state = null;
AnsiConsole.MarkupLine("[grey]Environment ready. You can now write C# code.[/]");
if (dbContextType != null)
{
    AnsiConsole.MarkupLine($"[grey]Tip: Instantiate your context like: var db = new {dbContextType.Name}();[/]");
}

while (true)
{
    var code = AnsiConsole.Ask<string>("[bold blue]>[/]");
    if (code == "exit") break;
    if (string.IsNullOrWhiteSpace(code)) continue;

    try
    {
        var sw = Stopwatch.StartNew();

        // Execute code
        if (state == null)
            state = await CSharpScript.RunAsync(code, options);
        else
            state = await state.ContinueWithAsync(code);

        sw.Stop();

        if (state.ReturnValue is { } returnValue)
        {
            // Format result nicely
            string resultText = returnValue.ToString() ?? "null";

            // If it's a list/enumerable, maybe print count or first few items? 
            // For now just ToString() is safe.

            AnsiConsole.Write(new Panel(resultText)
                .Header($"Result - {sw.ElapsedMilliseconds}ms")
                .Expand()
                .BorderColor(Color.Green));
        }
    }
    catch (CompilationErrorException e)
    {
        AnsiConsole.MarkupLine("[red]Compilation Error:[/]");
        foreach (var diagnostic in e.Diagnostics)
        {
            AnsiConsole.MarkupLine($"  [red]{diagnostic.GetMessage()}[/]");
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex);
    }
}
