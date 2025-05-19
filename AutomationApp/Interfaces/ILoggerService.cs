using System;
using AutomationApp.Models;
using Microsoft.Extensions.Logging;


namespace AutomationApp.Interfaces
{
    /// <summary>
    /// Defines the contract for logging operations in the automation framework.
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// Logs a message at the specified log level.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event ID.</param>
        /// <param name="state">The state object.</param>
        /// <param name="exception">The associated exception, if any.</param>
        /// <param name="formatter">The function to format the log message.</param>
        void Log<TState>(AutomationApp.Models.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

        /// <summary>
        /// Begins a logging scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="IDisposable"/> representing the scope.</returns>
        IDisposable BeginScope<TState>(TState state);

        /// <summary>
        /// Checks if the specified log level is enabled.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>True if the log level is enabled; otherwise, false.</returns>
        bool IsEnabled(AutomationApp.Models.LogLevel logLevel);

        /// <summary>
        /// Logs an error with an exception and optional message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The optional error message.</param>
        void LogError(Exception exception, string? message = null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message.</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message.</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a success message.
        /// </summary>
        /// <param name="message">The success message.</param>
        void LogSuccess(string message);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message.</param>
        void LogDebug(string message);
    }
}