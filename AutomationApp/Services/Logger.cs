using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;

namespace AutomationApp.Services;
public class Logger : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly string _logFile;
    private readonly string _errorFile;

    public Logger(string categoryName, ILogger innerLogger = null!)
    {
        _categoryName = categoryName;
        _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskAutomationTool", "Logs");
        _logFile = Path.Combine(_logDirectory, "automation.log");
        _errorFile = Path.Combine(_logDirectory, "error.log");
        _innerLogger = innerLogger;
        // _innerLogger = innerLogger ?? new NullLogger();
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return _innerLogger?.BeginScope(state) ?? NullScope.Instance;
    }

    private class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }

    public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}{Environment.NewLine}";

            // Log to file
            switch (logLevel)
            {
                case LogLevel.Error:
                case LogLevel.Critical:
                    File.AppendAllText(_errorFile, logEntry);
                    break;
                default:
                    File.AppendAllText(_logFile, logEntry);
                    break;
            }

            // Log to console
            if (logLevel >= LogLevel.Information)
            {
                Console.WriteLine(logEntry);
            }
            else if (logLevel == LogLevel.Error || logLevel == LogLevel.Critical)
            {
                Console.Error.WriteLine(logEntry);
            }

            // Log to inner logger
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to log message: {ex.Message}");
        }
    }

    public void LogError(Exception ex, string message = null!)
    {
        Log<object>(LogLevel.Error, 0, null!, ex, (s, e) => $"ERROR: {message ?? e?.Message ?? "Unknown error"}");
        _innerLogger.LogError(ex, message ?? ex.Message ?? "Unknown error");
    }

    public void LogWarning(string message)
    {
        Log<object>(LogLevel.Warning, 0, null!, null, (s, e) => message);
        _innerLogger.LogWarning(message);
    }

    public void LogInfo(string message)
    {
        Log<object>(LogLevel.Information, 0, null!, null, (s, e) => message);
        _innerLogger.LogInformation(message);
    }

    public void LogSuccess(string message)
    {
        Log<object>(LogLevel.Information, 0, null!, null, (s, e) => $"SUCCESS: {message}");
        _innerLogger.LogInformation($"SUCCESS: {message}");
    }

    public void LogDebug(string message)
    {
        Log<object>(LogLevel.Debug, 0, null!, null, (s, e) => message);
        _innerLogger.LogDebug(message);
    }

    public void LogTrace(string message)
    {
        Log<object>(LogLevel.Trace, 0, null!, null, (s, e) => message);
        _innerLogger.LogTrace(message);
    }
}
