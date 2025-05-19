namespace AutomationApp.Interfaces
{
    /// <summary>
    /// Defines the contract for scheduling automation tasks.
    /// </summary>
    public interface IAutomationScheduler : IDisposable
    {
        /// <summary>
        /// Starts the scheduler to execute tasks at regular intervals.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the scheduler from executing tasks.
        /// </summary>
        void Stop();
    }
}