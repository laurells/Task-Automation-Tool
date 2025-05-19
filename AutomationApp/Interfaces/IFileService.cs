using System.Threading.Tasks;

namespace AutomationApp.Services
{
    /// <summary>
    /// Defines the contract for file operations such as copying, moving, compressing, and validating files.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Copies a file from a source to a destination.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="destination">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CopyFileAsync(string source, string destination, bool overwrite = true);

        /// <summary>
        /// Moves a file from a source to a destination.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="destination">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MoveFileAsync(string source, string destination, bool overwrite = true);

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="filePath">The path to the file to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFileAsync(string filePath);

        /// <summary>
        /// Compares the MD5 hashes of two files.
        /// </summary>
        /// <param name="file1">The path to the first file.</param>
        /// <param name="file2">The path to the second file.</param>
        /// <returns>A task that resolves to true if the hashes match; otherwise, false.</returns>
        Task<bool> CompareFileHashesAsync(string file1, string file2);

        /// <summary>
        /// Compresses multiple files into a ZIP archive.
        /// </summary>
        /// <param name="files">The paths to the files to compress.</param>
        /// <param name="zipPath">The path to the output ZIP file.</param>
        /// <returns>A task that resolves to true if compression succeeds; otherwise, false.</returns>
        Task<bool> CompressFilesAsync(string[] files, string zipPath);

        /// <summary>
        /// Extracts files from a ZIP archive to a directory.
        /// </summary>
        /// <param name="zipPath">The path to the ZIP file.</param>
        /// <param name="extractPath">The directory to extract files to.</param>
        /// <returns>A task that resolves to true if extraction succeeds; otherwise, false.</returns>
        Task<bool> ExtractFilesAsync(string zipPath, string extractPath);

        /// <summary>
        /// Renames a file by moving it to a new path.
        /// </summary>
        /// <param name="oldPath">The current file path.</param>
        /// <param name="newPath">The new file path.</param>
        /// <returns>A task that resolves to true if renaming succeeds; otherwise, false.</returns>
        Task<bool> RenameFileAsync(string oldPath, string newPath);

        /// <summary>
        /// Creates a timestamped backup of a file.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="backupDirectory">The directory to store the backup.</param>
        /// <returns>A task that resolves to true if the backup succeeds; otherwise, false.</returns>
        Task<bool> BackupFileAsync(string source, string backupDirectory);

        /// <summary>
        /// Validates a file against an expected MD5 hash.
        /// </summary>
        /// <param name="filePath">The path to the file to validate.</param>
        /// <param name="expectedHash">The expected MD5 hash.</param>
        /// <returns>A task that resolves to true if the hash matches; otherwise, false.</returns>
        Task<bool> ValidateFileAsync(string filePath, string expectedHash);
    }
}