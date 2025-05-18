using Microsoft.Extensions.Logging;

namespace AutomationApp.Services
{
    public class NullLogger : ILogger
    {
        // Explicit interface implementation for BeginScope
        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Do nothing
        }

        public void LogError(Exception ex, string message = null!)
        {
            // Do nothing
        }

        public void LogWarning(string message)
        {
            // Do nothing
        }

        public void LogInfo(string message)
        {
            // Do nothing
        }

        public void LogSuccess(string message)
        {
            // Do nothing
        }

        public void LogDebug(string message)
        {
            // Do nothing
        }

        public void LogTrace(string message)
        {
            // Do nothing
        }

        // Singleton NullScope to return a no-op IDisposable
        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }
}