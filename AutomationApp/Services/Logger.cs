using Microsoft.Extensions.Logging;
using System;
using System.IO;

public class Logger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly string _logFile;
    private readonly string _errorFile;

    public Logger(string categoryName)
    {
        _categoryName = categoryName;
        _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskAutomationTool", "Logs");
        _logFile = Path.Combine(_logDirectory, "automation.log");
        _errorFile = Path.Combine(_logDirectory, "error.log");
        Directory.CreateDirectory(_logDirectory);
    }

    IDisposable ILogger.BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}{Environment.NewLine}";

            switch (logLevel)
            {
                case LogLevel.Error:
                case LogLevel.Critical:
                    File.AppendAllText(_errorFile, logEntry);
                    Console.Error.WriteLine(logEntry);
                    break;
                default:
                    File.AppendAllText(_logFile, logEntry);
                    Console.WriteLine(logEntry);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to log message: {ex.Message}");
        }
    }

    public void LogError(Exception ex, string message = null!)
    {
        Log<object>(LogLevel.Error, 0, null!, ex, (s, e) => $"ERROR: {message ?? e?.Message ?? "Unknown error"}");
    }

    public void LogWarning(string message)
    {
        Log<object>(LogLevel.Warning, 0, null!, null, (s, e) => message);
    }

    public void LogInfo(string message)
    {
        Log<object>(LogLevel.Information, 0, null!, null, (s, e) => message);
    }

    public void LogSuccess(string message)
    {
        Log<object>(LogLevel.Information, 0, null!, null, (s, e) => $"SUCCESS: {message}");
    }

    public void LogDebug(string message)
    {
        Log<object>(LogLevel.Debug, 0, null!, null, (s, e) => message);
    }

    public void LogTrace(string message)
    {
        Log<object>(LogLevel.Trace, 0, null!, null, (s, e) => message);
    }
}
