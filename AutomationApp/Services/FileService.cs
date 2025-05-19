using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using AutomationApp.Interfaces;
using System.Threading.Tasks;
using AutomationApp.Core;

namespace AutomationApp.Services
{
    /// <summary>
    /// A service for performing file operations such as copying, moving, compressing, and validating files.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IFileService"/> to provide file operations for the automation framework.
    /// Uses <see cref="System.IO"/> for file operations, <see cref="System.IO.Compression"/> for ZIP handling,
    /// and <see cref="System.Security.Cryptography"/> for MD5 hashing.
    /// </remarks>
    public class FileService : IFileService
    {
        private readonly Logger _logger; // Logger for recording operation details

        /// <summary>
        /// Initializes a new instance of the <see cref="FileService"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording operation details. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public FileService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Copies a file from a source to a destination.
        /// </summary>
        /// <param name="source">The source file path. Cannot be null or empty.</param>
        /// <param name="destination">The destination file path. Cannot be null or empty.</param>
        /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
        /// <exception cref="IOException">Thrown when the copy operation fails.</exception>
        public async Task CopyFileAsync(string source, string destination, bool overwrite = true)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source path cannot be null or empty.", nameof(source));
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path cannot be null or empty.", nameof(destination));
            if (!File.Exists(source))
            {
                _logger.LogInfo($"Source file not found: {source}");
                throw new FileNotFoundException($"Source file not found: {source}");
            }

            try
            {
                // Ensure destination directory exists
                var destDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                // Copy file asynchronously
                await Task.Run(() =>
                {
                    File.Copy(source, destination, overwrite);
                    _logger.LogSuccess($"Copied file: {source} -> {destination}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to copy file: {source} -> {destination}");
                throw new IOException($"Failed to copy file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Moves a file from a source to a destination.
        /// </summary>
        /// <param name="source">The source file path. Cannot be null or empty.</param>
        /// <param name="destination">The destination file path. Cannot be null or empty.</param>
        /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
        /// <exception cref="IOException">Thrown when the move operation fails or the destination exists and overwrite is false.</exception>
        public async Task MoveFileAsync(string source, string destination, bool overwrite = true)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source path cannot be null or empty.", nameof(source));
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path cannot be null or empty.", nameof(destination));
            if (!File.Exists(source))
            {
                _logger.LogInfo($"Source file not found: {source}");
                throw new FileNotFoundException($"Source file not found: {source}");
            }

            try
            {
                // Ensure destination directory exists
                var destDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                // Move file asynchronously
                await Task.Run(() =>
                {
                    if (File.Exists(destination) && !overwrite)
                    {
                        throw new IOException($"Destination file already exists: {destination}");
                    }
                    File.Move(source, destination);
                    _logger.LogSuccess($"Moved file: {source} -> {destination}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to move file: {source} -> {destination}");
                throw new IOException($"Failed to move file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="filePath">The path to the file to delete. Cannot be null or empty.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when the delete operation fails.</exception>
        public async Task DeleteFileAsync(string filePath)
        {
            // Validate input
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            try
            {
                // Delete file asynchronously
                await Task.Run(() =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogSuccess($"Deleted file: {filePath}");
                    }
                    else
                    {
                        _logger.LogWarning($"File not found for deletion: {filePath}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete file: {filePath}");
                throw new IOException($"Failed to delete file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Compares the MD5 hashes of two files.
        /// </summary>
        /// <param name="file1">The path to the first file. Cannot be null or empty.</param>
        /// <param name="file2">The path to the second file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if the hashes match; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="file1"/> or <paramref name="file2"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
        public async Task<bool> CompareFileHashesAsync(string file1, string file2)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(file1))
                throw new ArgumentException("First file path cannot be null or empty.", nameof(file1));
            if (string.IsNullOrEmpty(file2))
                throw new ArgumentException("Second file path cannot be null or empty.", nameof(file2));
            if (!File.Exists(file1))
            {
                _logger.LogInfo($"First file not found: {file1}");
                throw new FileNotFoundException($"First file not found: {file1}");
            }
            if (!File.Exists(file2))
            {
                _logger.LogInfo($"Second file not found: {file2}");
                throw new FileNotFoundException($"Second file not found: {file2}");
            }

            try
            {
                // Compute and compare hashes
                using var md5 = MD5.Create();
                var hash1 = await ComputeFileHashAsync(file1, md5);
                var hash2 = await ComputeFileHashAsync(file2, md5);
                bool areEqual = hash1.SequenceEqual(hash2);
                _logger.LogInfo($"Compared file hashes: {file1} and {file2}. Equal: {areEqual}");
                return areEqual;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to compare file hashes: {file1} and {file2}");
                return false;
            }
        }

        /// <summary>
        /// Compresses multiple files into a ZIP archive.
        /// </summary>
        /// <param name="files">The paths to the files to compress. Cannot be null.</param>
        /// <param name="zipPath">The path to the output ZIP file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if compression succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="files"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="zipPath"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when compression fails.</exception>
        public async Task<bool> CompressFilesAsync(string[] files, string zipPath)
        {
            // Validate inputs
            if (files == null)
                throw new ArgumentNullException(nameof(files));
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentException("ZIP path cannot be null or empty.", nameof(zipPath));

            try
            {
                // Ensure ZIP directory exists
                var zipDir = Path.GetDirectoryName(zipPath);
                if (!string.IsNullOrEmpty(zipDir))
                    Directory.CreateDirectory(zipDir);

                // Compress files asynchronously
                await Task.Run(() =>
                {
                    using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                    foreach (var file in files)
                    {
                        if (File.Exists(file))
                        {
                            archive.CreateEntryFromFile(file, Path.GetFileName(file));
                            _logger.LogSuccess($"Added to archive: {file} -> {zipPath}");
                        }
                        else
                        {
                            _logger.LogWarning($"File not found for compression: {file}");
                        }
                    }
                });
                _logger.LogSuccess($"Created ZIP archive: {zipPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to compress files to: {zipPath}");
                return false;
            }
        }

        /// <summary>
        /// Extracts files from a ZIP archive to a directory.
        /// </summary>
        /// <param name="zipPath">The path to the ZIP file. Cannot be null or empty.</param>
        /// <param name="extractPath">The directory to extract files to. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if extraction succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="zipPath"/> or <paramref name="extractPath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the ZIP file does not exist.</exception>
        /// <exception cref="IOException">Thrown when extraction fails.</exception>
        public async Task<bool> ExtractFilesAsync(string zipPath, string extractPath)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentException("ZIP path cannot be null or empty.", nameof(zipPath));
            if (string.IsNullOrEmpty(extractPath))
                throw new ArgumentException("Extract path cannot be null or empty.", nameof(extractPath));
            if (!File.Exists(zipPath))
            {
                _logger.LogInfo($"ZIP file not found: {zipPath}");
                throw new FileNotFoundException($"ZIP file not found: {zipPath}");
            }

            try
            {
                // Extract files asynchronously
                await Task.Run(() =>
                {
                    // Ensure extract directory exists
                    if (!Directory.Exists(extractPath))
                        Directory.CreateDirectory(extractPath);

                    using var archive = ZipFile.OpenRead(zipPath);
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.EndsWith("/"))
                        {
                            var destPath = Path.Combine(extractPath, entry.FullName);
                            var destDir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(destDir))
                                Directory.CreateDirectory(destDir);
                            entry.ExtractToFile(destPath, overwrite: true);
                            _logger.LogSuccess($"Extracted: {entry.FullName} -> {destPath}");
                        }
                    }
                });
                _logger.LogSuccess($"Extracted ZIP archive: {zipPath} -> {extractPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to extract archive: {zipPath} to {extractPath}");
                return false;
            }
        }

        /// <summary>
        /// Renames a file by moving it to a new path.
        /// </summary>
        /// <param name="oldPath">The current file path. Cannot be null or empty.</param>
        /// <param name="newPath">The new file path. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if renaming succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="oldPath"/> or <paramref name="newPath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
        /// <exception cref="IOException">Thrown when renaming fails.</exception>
        public async Task<bool> RenameFileAsync(string oldPath, string newPath)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(oldPath))
                throw new ArgumentException("Old path cannot be null or empty.", nameof(oldPath));
            if (string.IsNullOrEmpty(newPath))
                throw new ArgumentException("New path cannot be null or empty.", nameof(newPath));
            if (!File.Exists(oldPath))
            {
                _logger.LogInfo($"File not found for renaming: {oldPath}");
                throw new FileNotFoundException($"File not found: {oldPath}");
            }

            try
            {
                // Ensure new directory exists
                var newDir = Path.GetDirectoryName(newPath);
                if (!string.IsNullOrEmpty(newDir))
                    Directory.CreateDirectory(newDir);

                // Rename file asynchronously
                await Task.Run(() =>
                {
                    File.Move(oldPath, newPath);
                    _logger.LogSuccess($"Renamed file: {oldPath} -> {newPath}");
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to rename file: {oldPath} -> {newPath}");
                return false;
            }
        }

        /// <summary>
        /// Creates a timestamped backup of a file.
        /// </summary>
        /// <param name="source">The source file path. Cannot be null or empty.</param>
        /// <param name="backupDirectory">The directory to store the backup. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if the backup succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="backupDirectory"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
        /// <exception cref="IOException">Thrown when the backup operation fails.</exception>
        public async Task<bool> BackupFileAsync(string source, string backupDirectory)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source path cannot be null or empty.", nameof(source));
            if (string.IsNullOrEmpty(backupDirectory))
                throw new ArgumentException("Backup directory cannot be null or empty.", nameof(backupDirectory));
            if (!File.Exists(source))
            {
                _logger.LogInfo($"Source file not found: {source}");
                throw new FileNotFoundException($"Source file not found: {source}");
            }

            try
            {
                // Create backup path with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDirectory, $"{Path.GetFileNameWithoutExtension(source)}_backup_{timestamp}{Path.GetExtension(source)}");

                // Create backup directory if needed
                if (!Directory.Exists(backupDirectory))
                    Directory.CreateDirectory(backupDirectory);

                // Copy file as backup
                await CopyFileAsync(source, backupPath);
                _logger.LogSuccess($"Backed up file: {source} -> {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to backup file: {source} to {backupDirectory}");
                return false;
            }
        }

        /// <summary>
        /// Validates a file against an expected MD5 hash.
        /// </summary>
        /// <param name="filePath">The path to the file to validate. Cannot be null or empty.</param>
        /// <param name="expectedHash">The expected MD5 hash. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if the hash matches; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> or <paramref name="expectedHash"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        /// <exception cref="IOException">Thrown when hash computation fails.</exception>
        public async Task<bool> ValidateFileAsync(string filePath, string expectedHash)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (string.IsNullOrEmpty(expectedHash))
                throw new ArgumentException("Expected hash cannot be null or empty.", nameof(expectedHash));
            if (!File.Exists(filePath))
            {
                _logger.LogInfo($"File not found for validation: {filePath}");
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            try
            {
                // Compute file hash
                using var md5 = MD5.Create();
                var fileHash = await ComputeFileHashAsync(filePath, md5);
                var hashString = BitConverter.ToString(fileHash).Replace("-", "").ToLower();
                bool isValid = hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
                _logger.LogInfo($"Validated file: {filePath}. Hash match: {isValid}");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to validate file: {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Computes the MD5 hash of a file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>A task that resolves to the computed hash as a byte array.</returns>
        /// <exception cref="IOException">Thrown when reading the file fails.</exception>
        private async Task<byte[]> ComputeFileHashAsync(string filePath, HashAlgorithm algorithm)
        {
            // Compute hash asynchronously
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return await algorithm.ComputeHashAsync(stream);
        }
    }
}
//Implement LAter
// long totalBytes = new FileInfo(source).Length;
// long bytesCopied = 0;
// using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
// using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
// byte[] buffer = new byte[8192];
// int bytesRead;
// while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
// {
//     await destStream.WriteAsync(buffer.AsMemory(0, bytesRead));
//     bytesCopied += bytesRead;
//     _logger.LogDebug($"Copy progress: {bytesCopied}/{totalBytes} bytes");
// }