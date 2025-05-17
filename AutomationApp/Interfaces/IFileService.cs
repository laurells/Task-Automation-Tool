using System.Threading.Tasks;

namespace AutomationApp.Interfaces
{
    public interface IFileService
    {
        Task CopyFileAsync(string source, string destination, bool overwrite = true);
        Task MoveFileAsync(string source, string destination, bool overwrite = true);
        Task DeleteFileAsync(string filePath);
        Task<bool> CompareFileHashesAsync(string file1, string file2);
        Task<bool> CompressFilesAsync(string[] files, string zipPath);
        Task<bool> ExtractFilesAsync(string zipPath, string extractPath);
        Task<bool> RenameFileAsync(string oldPath, string newPath);
        Task<bool> BackupFileAsync(string source, string backupDirectory);
        Task<bool> ValidateFileAsync(string filePath, string expectedHash);
    }
}
