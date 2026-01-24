using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using System.Diagnostics;

namespace TinkNet;

public class ScriptEngine
{
    private ScriptState<object>? _state;
    private ScriptOptions _options;
    private readonly ScriptGlobals _globals;

    public ScriptEngine()
    {
        _globals = new ScriptGlobals();
        _options = ScriptOptions.Default
            .AddReferences(typeof(Enumerable).Assembly)
            .AddReferences(typeof(DbContext).Assembly)
            .AddReferences(typeof(ScriptGlobals).Assembly)
            .AddImports(
                "System", 
                "System.Linq", 
                "System.Collections.Generic", 
                "System.Threading.Tasks", 
                "Microsoft.EntityFrameworkCore", 
                "TinkNet"
            );
    }

    public void Initialize(Assembly targetAssembly, Type? dbContextType)
    {
        // Add main assembly
        _options = _options.AddReferences(targetAssembly);

        // Add dependencies found in the same folder
        var dependentReferences = Loader.GetAssemblyDependencies();
        if (dependentReferences.Any())
        {
            _options = _options.AddReferences(dependentReferences);
        }

        // Auto-import all public namespaces from ALL assemblies (Main + Dependencies)
        var allAssemblies = new List<Assembly> { targetAssembly };
        if (dependentReferences.Any())
        {
            allAssemblies.AddRange(dependentReferences);
        }

        var namespaces = Loader.GetPublicNamespacesFromAssemblies(allAssemblies);
        _options = _options.AddImports(namespaces);

        // Also import DbContext namespace explicitly if found (just to be safe, though likely covered above)
        if (dbContextType?.Namespace != null)
        {
            _options = _options.AddImports(dbContextType.Namespace);
        }
    }

    public async Task RunAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;

        try
        {
            var sw = Stopwatch.StartNew();

            if (_state == null)
            {
                _state = await CSharpScript.RunAsync(code, _options, globals: _globals, globalsType: typeof(ScriptGlobals));
            }
            else
            {
                _state = await _state.ContinueWithAsync(code);
            }

            sw.Stop();

            if (_state.ReturnValue != null)
            {
                StartupWizard.DisplayResult(_state.ReturnValue, sw.ElapsedMilliseconds);
            }
        }
        catch (CompilationErrorException e)
        {
            StartupWizard.DisplayError("Compilation Error:", string.Join("\n", e.Diagnostics.Select(d => d.GetMessage())));
        }
        catch (Exception ex)
        {
            StartupWizard.DisplayException(ex);
        }
    }
}
