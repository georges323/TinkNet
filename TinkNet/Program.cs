using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Spectre.Console;
using System.Diagnostics;

var options = ScriptOptions.Default
    .AddReferences(typeof(Enumerable).Assembly)
    .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Threading.Tasks");

ScriptState<object>? state = null;

AnsiConsole.Write(new FigletText("TinkNet").Color(Color.Green));

while (true)
{
    var code = AnsiConsole.Ask<string>("[bold blue]>[/]");
    if (code == "exit") break;

    try
    {
        var sw = Stopwatch.StartNew();

        // Execute code
        // If state is null, we start a new session. Otherwise, we continue from the last state.
        if (state == null)
            state = await CSharpScript.RunAsync(code, options);
        else
            state = await state.ContinueWithAsync(code);

        sw.Stop();

        if (state.ReturnValue is { } returnValue && returnValue.ToString() is { } result)
        {
            AnsiConsole.Write(new Panel(result)
                .Header($"Result - {sw.ElapsedMilliseconds}ms")
                .Expand()
                .BorderColor(Color.Green));
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex);
    }
}