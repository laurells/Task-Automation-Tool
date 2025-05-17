using Microsoft.Extensions.Logging;

namespace AutomationApp.Services
{
    public class NullLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null!;

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
    }
}
