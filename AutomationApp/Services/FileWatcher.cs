using System;
using System.IO;
using System.Threading.Tasks;
using AutomationApp.Core;
using AutomationApp.Interfaces;

namespace AutomationApp.Services
{
    /// <summary>
    /// A service for monitoring file system changes and moving new files to a destination.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IFileWatcherService"/> to provide file monitoring for the automation framework.
    /// Uses <see cref="FileSystemWatcher"/> for real-time file detection and <see cref="FileService"/> for file operations.
    /// </remarks>
    public class FileWatcherService : IFileWatcherService
    {
        private readonly FileService _fileService; // Service for file operations
        private readonly Logger _logger;           // Logger for recording operation details
        private FileSystemWatcher? _watcher;       // Watcher for file system events
        private bool _disposed;                   // Tracks disposal state

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWatcherService"/> class.
        /// </summary>
        /// <param name="fileService">The file service for moving files. Cannot be null.</param>
        /// <param name="logger">The logger for recording operation details. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileService"/> or <paramref name="logger"/> is null.</exception>
        public FileWatcherService(FileService fileService, Logger logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts monitoring a folder for new files of a specified type and moves them to a destination.
        /// </summary>
        /// <param name="folderPath">The folder to monitor. Cannot be null or empty.</param>
        /// <param name="fileType">The file extension to monitor (e.g., "txt"). Cannot be null or empty.</param>
        /// <param name="destination">The destination folder for moving files. Cannot be null or empty.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="folderPath"/>, <paramref name="fileType"/>, or <paramref name="destination"/> is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the folder or destination does not exist.</exception>
        /// <remarks>
        /// Monitors for new files matching the specified extension and moves them to the destination using <see cref="FileService.MoveFileAsync"/>.
        /// </remarks>
        public async Task StartWatchingAsync(string folderPath, string fileType, string destination)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
            if (string.IsNullOrEmpty(fileType))
                throw new ArgumentException("File type cannot be null or empty.", nameof(fileType));
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path cannot be null or empty.", nameof(destination));
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInfo($"Folder not found: {folderPath}");
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }
            if (!Directory.Exists(destination))
            {
                _logger.LogInfo($"Destination folder not found: {destination}");
                throw new DirectoryNotFoundException($"Destination folder not found: {destination}");
            }

            try
            {
                // Clean file type (remove leading dot if present)
                var cleanFileType = fileType.StartsWith(".") ? fileType.Substring(1) : fileType;

                // Initialize watcher
                _watcher = new FileSystemWatcher
                {
                    Path = folderPath,
                    Filter = $"*.{cleanFileType}",
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };

                // Set up event handler for new files
                _watcher.Created += async (sender, e) =>
                {
                    _logger.LogInfo($"New file detected: {e.FullPath}");
                    var destPath = Path.Combine(destination, Path.GetFileName(e.FullPath));

                    // Retry move operation to handle file locks
                    bool moved = await RetryFileMoveAsync(e.FullPath, destPath, maxAttempts: 3, delayMs: 1000);
                    if (!moved)
                    {
                        _logger.LogInfo($"Failed to move file after retries: {e.FullPath} -> {destPath}");
                    }
                };

                _logger.LogInfo($"Started watching '{folderPath}' for '*.{cleanFileType}' files");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start watching folder: {folderPath}");
                throw;
            }
        }

        /// <summary>
        /// Stops monitoring the folder and disposes of the watcher.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Safely stops the file system watcher and releases resources.
        /// </remarks>
        public async Task StopWatchingAsync()
        {
            if (_watcher == null)
            {
                _logger.LogWarning("File watcher is not running.");
                return;
            }

            try
            {
                // Disable events and dispose asynchronously
                await Task.Run(() =>
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                    _watcher = null;
                    _logger.LogInfo("Stopped file watcher.");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop file watcher.");
            }
        }

        /// <summary>
        /// Disposes of the file watcher resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Synchronously dispose watcher
                _watcher?.Dispose();
                _watcher = null;
                _logger.LogInfo("Disposed file watcher service.");
                _disposed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal of file watcher service.");
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retries moving a file with delays to handle file locks.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="destination">The destination file path.</param>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="delayMs">The delay between attempts in milliseconds.</param>
        /// <returns>A task that resolves to true if the move succeeds; otherwise, false.</returns>
        private async Task<bool> RetryFileMoveAsync(string source, string destination, int maxAttempts, int delayMs)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await _fileService.MoveFileAsync(source, destination);
                    return true;
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    _logger.LogWarning($"Attempt {attempt}/{maxAttempts} failed to move file: {source}. Retrying after {delayMs}ms.");
                    if (attempt < maxAttempts)
                        await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to move file: {source} -> {destination}");
                    return false;
                }
            }
            return false;
        }
    }
}