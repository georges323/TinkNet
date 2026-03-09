using System.Reflection;
using Microsoft.Extensions.Configuration;
using TinkNet;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

var configuration = builder.Build();

StartupWizard.ShowHeader();

var dllPath = StartupWizard.AskForDllPath(configuration);

Assembly assembly;
try
{
    assembly = Loader.Load(dllPath);
    StartupWizard.DisplayResult($"Successfully loaded assembly: {assembly.GetName().Name}", 0);
}
catch (Exception ex)
{
    StartupWizard.DisplayError("Failed to load assembly:", ex.Message);
    return;
}

// Initialize Engine
var engine = new ScriptEngine();
var dbContextType = Loader.FindDbContextFromAssembly(assembly);

engine.Initialize(assembly, dbContextType);

// Configure DB if applicable
if (dbContextType != null)
{
    var setupScript = StartupWizard.AskToConfigureDatabase(dbContextType, configuration);
    if (setupScript != null)
    {
        await engine.RunAsync(setupScript);
    }
    else
    {
        StartupWizard.ShowDbHelp(dbContextType);
    }
}
else
{
    StartupWizard.DisplayError("Notice", "No concrete DbContext found in this DLL.");
}

// REPL Loop
StartupWizard.DisplayResult("Environment ready. You can now write C# code.", 0);

while (true)
{
    var code = ReplPrompt.ReadCode();
    if (code == "exit") break;
    
    await engine.RunAsync(code);
}
