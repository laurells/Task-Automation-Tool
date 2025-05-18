using Microsoft.Extensions.Logging;
using AutomationApp.Models;
using MicrosoftLogging = Microsoft.Extensions.Logging; // Alias for clarity

namespace AutomationApp.Services
{
    public class Logger
    {
        private readonly ILogger _innerLogger;
        private readonly string _categoryName;
        private readonly string _logDirectory;
        private readonly string _logFile;
        private readonly string _errorFile;
        private readonly LoggingConfiguration _loggingConfig;

        public Logger(string categoryName, LoggingConfiguration loggingConfig, ILogger? innerLogger = null)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _loggingConfig = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));
            _logDirectory = _loggingConfig.LogDirectory;
            _logFile = Path.Combine(_logDirectory, "automation.log");
            _errorFile = Path.Combine(_logDirectory, "error.log");
            _innerLogger = innerLogger ?? CreateInnerLogger(categoryName, loggingConfig);

            // Ensure log directory exists
            try
            {
                Directory.CreateDirectory(_logDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create log directory {_logDirectory}: {ex.Message}");
            }
        }

        private ILogger CreateInnerLogger(string categoryName, LoggingConfiguration config)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                // Map custom AutomationApp.Models.LogLevel to Microsoft.Extensions.Logging.LogLevel
                var logLevel = config.LogLevel switch
                {
                    AutomationApp.Models.LogLevel.Debug => MicrosoftLogging.LogLevel.Debug,
                    AutomationApp.Models.LogLevel.Info => MicrosoftLogging.LogLevel.Information,
                    AutomationApp.Models.LogLevel.Warning => MicrosoftLogging.LogLevel.Warning,
                    AutomationApp.Models.LogLevel.Error => MicrosoftLogging.LogLevel.Error,
                    AutomationApp.Models.LogLevel.Success => MicrosoftLogging.LogLevel.Information, // Map Success to Information
                    _ => MicrosoftLogging.LogLevel.Information
                };

                builder.SetMinimumLevel(logLevel);

                if (config.EnableConsoleOutput)
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    });
                }
            });
            return loggerFactory.CreateLogger(categoryName);
        }

        IDisposable BeginScope<TState>(TState state)
        {
            return BeginScope(state) ?? NullScope.Instance;
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }

        public bool IsEnabled(AutomationApp.Models.LogLevel logLevel) => _innerLogger.IsEnabled(ConvertLogLevel(logLevel));

        private MicrosoftLogging.LogLevel ConvertLogLevel(AutomationApp.Models.LogLevel logLevel) => logLevel switch
        {
            AutomationApp.Models.LogLevel.Debug => MicrosoftLogging.LogLevel.Debug,
            AutomationApp.Models.LogLevel.Info => MicrosoftLogging.LogLevel.Information,
            AutomationApp.Models.LogLevel.Warning => MicrosoftLogging.LogLevel.Warning,
            AutomationApp.Models.LogLevel.Error => MicrosoftLogging.LogLevel.Error,
            AutomationApp.Models.LogLevel.Success => MicrosoftLogging.LogLevel.Information,
            _ => MicrosoftLogging.LogLevel.Information
        };

        public void Log<TState>(AutomationApp.Models.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var message = formatter(state, exception);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}{Environment.NewLine}";

                // Log to file if enabled
                if (_loggingConfig.EnableFileLogging)
                {
                    switch (logLevel)
                    {
                        case AutomationApp.Models.LogLevel.Error when _loggingConfig.EnableErrorLogging:
                        case AutomationApp.Models.LogLevel.Success when _loggingConfig.EnableErrorLogging: // Treat Success as Error for error.log
                            File.AppendAllText(_errorFile, logEntry);
                            break;
                        default:
                            File.AppendAllText(_logFile, logEntry);
                            break;
                    }
                }

                // Log to console if enabled
                if (_loggingConfig.EnableConsoleOutput)
                {
                    if (logLevel >= AutomationApp.Models.LogLevel.Info)
                    {
                        Console.WriteLine(logEntry);
                    }
                    else if (logLevel == AutomationApp.Models.LogLevel.Error)
                    {
                        Console.Error.WriteLine(logEntry);
                    }
                }

                // Log to inner logger
                _innerLogger.Log(ConvertLogLevel(logLevel), eventId, state, exception, formatter);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to log message: {ex.Message}");
            }
        }

        public void LogError(Exception ex, string message = null!)
        {
            Log<object>(AutomationApp.Models.LogLevel.Error, 0, null!, ex, (s, e) => $"ERROR: {message ?? e?.Message ?? "Unknown error"}");
            _innerLogger.LogError(ex, message ?? ex?.Message ?? "Unknown error");
        }

        public void LogWarning(string message)
        {
            Log<object>(AutomationApp.Models.LogLevel.Warning, 0, null!, null, (s, e) => message);
            _innerLogger.LogWarning(message);
        }

        public void LogInfo(string message)
        {
            Log<object>(AutomationApp.Models.LogLevel.Info, 0, null!, null, (s, e) => message);
            _innerLogger.LogInformation(message);
        }

        public void LogSuccess(string message)
        {
            Log<object>(AutomationApp.Models.LogLevel.Success, 0, null!, null, (s, e) => $"SUCCESS: {message}");
            _innerLogger.LogInformation($"SUCCESS: {message}");
        }

        public void LogDebug(string message)
        {
            Log<object>(AutomationApp.Models.LogLevel.Debug, 0, null!, null, (s, e) => message);
            _innerLogger.LogDebug(message);
        }

        public void LogTrace(string message)
        {
            Log<object>(AutomationApp.Models.LogLevel.Trace, 0, null!, null, (s, e) => message);
            _innerLogger.LogTrace(message);
        }
    }
}