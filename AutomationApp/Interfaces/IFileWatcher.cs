using System.Threading.Tasks;

namespace AutomationApp.Interfaces
{
    /// <summary>
    /// Defines the contract for monitoring file system changes and processing new files.
    /// </summary>
    public interface IFileWatcherService : IDisposable
    {
        /// <summary>
        /// Starts monitoring a folder for new files of a specified type and moves them to a destination.
        /// </summary>
        /// <param name="folderPath">The folder to monitor.</param>
        /// <param name="fileType">The file extension to monitor (e.g., "txt").</param>
        /// <param name="destination">The destination folder for moving files.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartWatchingAsync(string folderPath, string fileType, string destination);

        /// <summary>
        /// Stops monitoring the folder and disposes of the watcher.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StopWatchingAsync();
    }
}