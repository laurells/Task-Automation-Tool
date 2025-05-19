using System;
using System.IO;
using System.Threading.Tasks;
using AutomationApp.Core;
using AutomationApp.Models;
using Microsoft.Extensions.Logging;
using MicrosoftLogging = Microsoft.Extensions.Logging;
using AutomationApp.Interfaces;

namespace AutomationApp.Services
{
    /// <summary>
    /// A custom logging service for the automation framework, wrapping Microsoft.Extensions.Logging.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="ILoggerService"/> to provide logging to console and files based on <see cref="LoggingConfiguration"/>.
    /// Supports custom log levels defined in <see cref="AutomationApp.Models.LogLevel"/>.
    /// </remarks>
    public class Logger : ILoggerService
    {
        private readonly ILogger _innerLogger;            // Underlying Microsoft.Extensions.Logging logger
        private readonly string _categoryName;            // Logger category (e.g., service name)
        private readonly string _logDirectory;            // Directory for log files
        private readonly string _logFile;                 // Path to general log file
        private readonly string _errorFile;               // Path to error log file
        private readonly LoggingConfiguration _loggingConfig; // Logging configuration

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="categoryName">The category name for the logger. Cannot be null or empty.</param>
        /// <param name="loggingConfig">The logging configuration. Cannot be null.</param>
        /// <param name="innerLogger">The optional underlying logger. If null, a new logger is created.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="categoryName"/> or <paramref name="loggingConfig"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="categoryName"/> is empty or <paramref name="loggingConfig.LogDirectory"/> is invalid.</exception>
        public Logger(string categoryName, LoggingConfiguration loggingConfig, ILogger? innerLogger = null)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));
            if (loggingConfig == null)
                throw new ArgumentNullException(nameof(loggingConfig));
            if (string.IsNullOrEmpty(loggingConfig.LogDirectory))
                throw new ArgumentException("Log directory cannot be null or empty.", nameof(loggingConfig));

            _categoryName = categoryName;
            _loggingConfig = loggingConfig;
            _logDirectory = loggingConfig.LogDirectory;
            _logFile = Path.Combine(_logDirectory, "automation.log");
            _errorFile = Path.Combine(_logDirectory, "error.log");
            _innerLogger = innerLogger ?? CreateInnerLogger(categoryName, loggingConfig);

            // Ensure log directory exists
            try
            {
                Directory.CreateDirectory(_logDirectory);
                _innerLogger.LogInformation($"Created log directory: {_logDirectory}");
            }
            catch (Exception ex)
            {
                _innerLogger.LogError(ex, $"Failed to create log directory: {_logDirectory}");
            }
        }

        /// <summary>
        /// Creates an underlying Microsoft.Extensions.Logging logger based on configuration.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <param name="config">The logging configuration.</param>
        /// <returns>An <see cref="ILogger"/> instance.</returns>
        private ILogger CreateInnerLogger(string categoryName, LoggingConfiguration config)
        {
            // Map custom LogLevel to Microsoft.Extensions.Logging.LogLevel
            var logLevel = ConvertLogLevel(config.LogLevel);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
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

        /// <summary>
        /// Converts a custom LogLevel to Microsoft.Extensions.Logging.LogLevel.
        /// </summary>
        /// <param name="logLevel">The custom log level.</param>
        /// <returns>The corresponding Microsoft.Extensions.Logging.LogLevel.</returns>
        private MicrosoftLogging.LogLevel ConvertLogLevel(AutomationApp.Models.LogLevel logLevel) => logLevel switch
        {
            AutomationApp.Models.LogLevel.Debug => MicrosoftLogging.LogLevel.Debug,
            AutomationApp.Models.LogLevel.Info => MicrosoftLogging.LogLevel.Information,
            AutomationApp.Models.LogLevel.Warning => MicrosoftLogging.LogLevel.Warning,
            AutomationApp.Models.LogLevel.Error => MicrosoftLogging.LogLevel.Error,
            AutomationApp.Models.LogLevel.Success => MicrosoftLogging.LogLevel.Information,
            _ => MicrosoftLogging.LogLevel.Information
        };

        /// <summary>
        /// Logs a message at the specified log level.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="state">The state object.</param>
        /// <param name="exception">The associated exception, if any.</param>
        /// <param name="formatter">The function to format the log message.</param>
        /// <remarks>
        /// Logs to console, general log file, or error log file based on <see cref="LoggingConfiguration"/>.
        /// </remarks>
        public void Log<TState>(AutomationApp.Models.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                // Format log message
                var message = formatter(state, exception);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}{Environment.NewLine}";

                // Log to file asynchronously if enabled
                if (_loggingConfig.EnableFileLogging)
                {
                    var targetFile = logLevel == AutomationApp.Models.LogLevel.Error && _loggingConfig.EnableErrorLogging ? _errorFile : _logFile;
                    File.AppendAllTextAsync(targetFile, logEntry).GetAwaiter().GetResult(); // Synchronous for simplicity; consider async in high-throughput scenarios
                }

                // Log to inner logger
                _innerLogger.Log(ConvertLogLevel(logLevel), eventId, state, exception, formatter);
            }
            catch (Exception ex)
            {
                _innerLogger.LogError(ex, "Failed to log message");
            }
        }

        /// <summary>
        /// Begins a logging scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="IDisposable"/> representing the scope.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return _innerLogger.BeginScope(state) ?? NullScope.Instance;
        }

        /// <summary>
        /// Checks if the specified log level is enabled.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>True if the log level is enabled; otherwise, false.</returns>
        public bool IsEnabled(AutomationApp.Models.LogLevel logLevel)
        {
            return _innerLogger.IsEnabled(ConvertLogLevel(logLevel));
        }

        /// <summary>
        /// Logs an error with an exception and optional message.
        /// </summary>
        /// <param name="exception">The exception to log. Cannot be null.</param>
        /// <param name="message">The optional error message.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public void LogError(Exception exception, string? message = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            Log(AutomationApp.Models.LogLevel.Error, 0, (object?)null, exception, (_, ex) => $"ERROR: {message ?? ex?.Message ?? "Unknown error"}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message. Cannot be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or empty.</exception>
        public void LogWarning(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            Log(AutomationApp.Models.LogLevel.Warning, 0, (object?)null, null, (_, _) => message);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message. Cannot be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or empty.</exception>
        public void LogInfo(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            Log(AutomationApp.Models.LogLevel.Info, 0, (object?)null, null, (_, _) => message);
        }

        /// <summary>
        /// Logs a success message.
        /// </summary>
        /// <param name="message">The success message. Cannot be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or empty.</exception>
        public void LogSuccess(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            Log(AutomationApp.Models.LogLevel.Success, 0, (object?)null, null, (_, _) => $"SUCCESS: {message}");
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message. Cannot be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or empty.</exception>
        public void LogDebug(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            Log(AutomationApp.Models.LogLevel.Debug, 0, (object?)null, null, (_, _) => message);
        }

        /// <summary>
        /// A null scope implementation for when scope creation fails.
        /// </summary>
        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}