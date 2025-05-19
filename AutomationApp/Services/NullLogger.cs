using System;
using AutomationApp.Models;
using Microsoft.Extensions.Logging;
using MicrosoftLogging = Microsoft.Extensions.Logging;
using AutomationApp.Interfaces;


namespace AutomationApp.Services
{
    /// <summary>
    /// A no-op logger that implements <see cref="ILoggerService"/> and <see cref="ILogger"/> for scenarios where logging is disabled.
    /// </summary>
    /// <remarks>
    /// Follows the null object pattern to silently discard log messages, ensuring compatibility with services expecting
    /// <see cref="ILoggerService"/> or <see cref="Microsoft.Extensions.Logging.ILogger"/>. Used in testing or configurations
    /// where logging is not required.
    /// </remarks>
    public class NullLogger : ILoggerService
    {
        /// <summary>
        /// Logs a message at the specified log level. This implementation does nothing.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="state">The state object.</param>
        /// <param name="exception">The associated exception, if any.</param>
        /// <param name="formatter">The function to format the log message.</param>
        public void Log<TState>(AutomationApp.Models.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // No-op: silently discard the log message
        }

        /// <summary>
        /// Begins a logging scope. Returns a no-op scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="IDisposable"/> representing a no-op scope.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        /// <summary>
        /// Checks if the specified log level is enabled. Always returns false.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>Always false, indicating no log levels are enabled.</returns>
        public bool IsEnabled(AutomationApp.Models.LogLevel logLevel)
        {
            return false;
        }

        /// <summary>
        /// Logs an error with an exception and optional message. This implementation does nothing.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The optional error message.</param>
        public void LogError(Exception exception, string? message = null)
        {
            // No-op: silently discard the error log
        }

        /// <summary>
        /// Logs a warning message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The warning message.</param>
        public void LogWarning(string message)
        {
            // No-op: silently discard the warning log
        }

        /// <summary>
        /// Logs an informational message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The informational message.</param>
        public void LogInfo(string message)
        {
            // No-op: silently discard the info log
        }

        /// <summary>
        /// Logs a success message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The success message.</param>
        public void LogSuccess(string message)
        {
            // No-op: silently discard the success log
        }

        /// <summary>
        /// Logs a debug message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The debug message.</param>
        public void LogDebug(string message)
        {
            // No-op: silently discard the debug log
        }

        /// <summary>
        /// Explicit implementation of <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope{TState}"/>.
        /// Returns a no-op scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="IDisposable"/> representing a no-op scope.</returns>
        // IDisposable? ILogger.BeginScope<TState>(TState state)

        // {
        //     return NullScope.Instance;
        // }

        // /// <summary>
        // /// Explicit implementation of <see cref="Microsoft.Extensions.Logging.ILogger.IsEnabled"/>.
        // /// Always returns false.
        // /// </summary>
        // /// <param name="logLevel">The log level to check.</param>
        // /// <returns>Always false, indicating no log levels are enabled.</returns>
        // bool ILogger.IsEnabled(AutomationApp.Models.LogLevel logLevel)
        // {
        //     return false;
        // }

        /// <summary>
        /// Explicit implementation of <see cref="Microsoft.Extensions.Logging.ILogger.Log{TState}"/>.
        /// This implementation does nothing.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="state">The state object.</param>
        /// <param name="exception">The associated exception, if any.</param>
        /// <param name="formatter">The function to format the log message.</param>
        // void ILogger.Log<TState>(AutomationApp.Models.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        // {
        //     // No-op: silently discard the log message
        // }

        /// <summary>
        /// A singleton no-op scope implementation for null logging.
        /// </summary>
        private class NullScope : IDisposable
        {
            /// <summary>
            /// The singleton instance of <see cref="NullScope"/>.
            /// </summary>
            public static readonly NullScope Instance = new();

            /// <summary>
            /// Disposes the scope. This implementation does nothing.
            /// </summary>
            public void Dispose()
            {
                // No-op: no resources to release
            }
        }
    }
}