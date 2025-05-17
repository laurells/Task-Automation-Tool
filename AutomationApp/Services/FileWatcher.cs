using System;
using System.IO;
using System.Threading.Tasks;

namespace AutomationApp.Services
{
    public class FileWatcherService(FileService fileService)
    {
        private FileSystemWatcher? _watcher;
        private readonly FileService _fileService = fileService;

        public void StartWatching(string folderPath, string fileType, string destination)
        {
            _watcher = new FileSystemWatcher(folderPath, $"*.{fileType}")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            _watcher.Created += async (s, e) =>
            {
                Console.WriteLine($"New file detected: {e.FullPath}");
                await Task.Delay(1000); // Give system time to release lock

                FileService.MoveFile(e.FullPath, Path.Combine(destination, Path.GetFileName(e.FullPath)));
            };

            Console.WriteLine($"Watching '{folderPath}' for '*.{fileType}' files...");
        }

        public void StopWatching()
        {
            _watcher?.Dispose();
        }
    }
}
