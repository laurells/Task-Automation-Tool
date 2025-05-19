using System.Timers;
using Timer = System.Timers.Timer;
using AutomationApp.Core;
using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp
{
    /// <summary>
    /// Schedules periodic execution of automation rules using a timer.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IAutomationScheduler"/> to integrate with the automation framework.
    /// Uses a <see cref="System.Timers.Timer"/> to trigger <see cref="AutomationEngine.ExecuteAllAsync"/> at regular intervals.
    /// </remarks>
    public class AutomationScheduler : IAutomationScheduler
    {
        private readonly AutomationEngine _engine; // Engine for executing rules
        private readonly Timer _timer;             // Timer for scheduling
        private readonly Logger _logger;           // Logger for execution details
        private bool _disposed;                   // Tracks disposal state

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationScheduler"/> class.
        /// </summary>
        /// <param name="engine">The automation engine to execute rules. Cannot be null.</param>
        /// <param name="intervalSeconds">The interval between executions in seconds. Must be positive.</param>
        /// <param name="logger">The logger for recording scheduler events. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> or <paramref name="logger"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="intervalSeconds"/> is not positive.</exception>
        public AutomationScheduler(AutomationEngine engine, int intervalSeconds, Logger logger)
        {
            // Validate inputs
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (intervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Interval must be positive.");

            _engine = engine;
            _logger = logger;
            _timer = new Timer(intervalSeconds * 1000); // Convert seconds to milliseconds
            _timer.Elapsed += TimerElapsedAsync; // Attach event handler
        }

        /// <summary>
        /// Starts the scheduler to execute tasks at regular intervals.
        /// </summary>
        /// <remarks>
        /// Begins the timer to trigger <see cref="AutomationEngine.ExecuteAllAsync"/> periodically.
        /// Logs the start event.
        /// </remarks>
        public void Start()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
                _logger.LogInfo("AutomationScheduler started.");
            }
            else
            {
                _logger.LogWarning("AutomationScheduler is already running.");
            }
        }

        /// <summary>
        /// Stops the scheduler from executing tasks.
        /// </summary>
        /// <remarks>
        /// Stops the timer and logs the stop event.
        /// </remarks>
        public void Stop()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _logger.LogInfo("AutomationScheduler stopped.");
            }
            else
            {
                _logger.LogWarning("AutomationScheduler is already stopped.");
            }
        }

        /// <summary>
        /// Disposes of the scheduler, releasing resources.
        /// </summary>
        /// <remarks>
        /// Stops and disposes the timer to prevent resource leaks.
        /// </remarks>
        public void Dispose()
        {
            if (!_disposed)
            {
                _timer.Stop();
                _timer.Elapsed -= TimerElapsedAsync;
                _timer.Dispose();
                _disposed = true;
                _logger.LogInfo("AutomationScheduler disposed.");
            }
        }

        /// <summary>
        /// Handles the timer elapsed event by executing all automation rules.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// Executes <see cref="AutomationEngine.ExecuteAllAsync"/> and logs any errors.
        /// </remarks>
        private async void TimerElapsedAsync(object? sender, ElapsedEventArgs e)
        {
            try
            {
                _logger.LogDebug("AutomationScheduler executing rules.");
                await _engine.ExecuteAllAsync();
                _logger.LogDebug("AutomationScheduler completed rule execution.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing automation rules in scheduler.");
            }
        }
    }
}