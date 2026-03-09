using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace TinkNet;

public class SqlConsoleLoggerProvider : ILoggerProvider
{
    private readonly ScriptGlobals _globals;

    public SqlConsoleLoggerProvider(ScriptGlobals globals)
    {
        _globals = globals;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SqlConsoleLogger(categoryName, _globals);
    }

    public void Dispose() { }
}

public class SqlConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ScriptGlobals _globals;

    public SqlConsoleLogger(string categoryName, ScriptGlobals globals)
    {
        _categoryName = categoryName;
        _globals = globals;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        // Only log EF Core Database Command execution
        return _globals.ShowSql && _categoryName == "Microsoft.EntityFrameworkCore.Database.Command" && logLevel == LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        // eventId.Id == 20101 is typically ExecutedDbCommand in EF Core
        if (eventId.Id == 20101) 
        {
            var logMessage = formatter(state, exception);
            
            // EF Core's default log message includes execution time and then the SQL.
            // Let's print it nicely.
            AnsiConsole.MarkupLine("\n[grey]==================== SQL EXECUTED ====================[/]");
            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(logMessage)}[/]");
            AnsiConsole.MarkupLine("[grey]======================================================[/]\n");
        }
    }
}