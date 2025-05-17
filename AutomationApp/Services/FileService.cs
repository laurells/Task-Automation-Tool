using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutomationApp.Services;

namespace AutomationApp.Services
{
    public class FileService(Logger logger)
    {
        private readonly Logger _logger = logger;

        public async Task CopyFileAsync(string source, string destination, bool overwrite = true)
        {
            try
            {
                await Task.Run(() =>
                {
                    File.Copy(source, destination, overwrite);
                    _logger.LogSuccess($"Copied: {source} -> {destination}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to copy file: {source}");
                throw;
            }
        }

        public async Task MoveFileAsync(string source, string destination, bool overwrite = true)
        {
            try
            {
                await Task.Run(() =>
                {
                    File.Move(source, destination, overwrite);
                    _logger.LogSuccess($"Moved: {source} -> {destination}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to move file: {source}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
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
                throw;
            }
        }

        public async Task<bool> CompareFileHashesAsync(string file1, string file2)
        {
            try
            {
                using var md5 = MD5.Create();
                var hash1 = await ComputeFileHashAsync(file1, md5);
                var hash2 = await ComputeFileHashAsync(file2, md5);
                return hash1.SequenceEqual(hash2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to compare file hashes: {file1} and {file2}");
                return false;
            }
        }

        private async Task<byte[]> ComputeFileHashAsync(string filePath, HashAlgorithm algorithm)
        {
            using var stream = File.OpenRead(filePath);
            return await algorithm.ComputeHashAsync(stream);
        }

        public async Task<bool> CompressFilesAsync(string[] files, string zipPath)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var archive = System.IO.Compression.ZipFile.Open(zipPath, ZipArchiveMode.Create);
                    foreach (var file in files)
                    {
                        if (File.Exists(file))
                        {
                            archive.CreateEntryFromFile(file, Path.GetFileName(file));
                            _logger.LogSuccess($"Added to archive: {file}");
                        }
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to compress files to: {zipPath}");
                return false;
            }
        }

        public async Task<bool> ExtractFilesAsync(string zipPath, string extractPath)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                    }

                    using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.EndsWith("/"))
                        {
                            var destPath = Path.Combine(extractPath, entry.FullName);
                            var destDir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                            }
                            entry.ExtractToFile(destPath, overwrite: true);
                            _logger.LogSuccess($"Extracted: {entry.FullName}");
                        }
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to extract archive: {zipPath}");
                return false;
            }
        }

        public async Task<bool> RenameFileAsync(string oldPath, string newPath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (File.Exists(oldPath))
                    {
                        File.Move(oldPath, newPath);
                        _logger.LogSuccess($"Renamed: {oldPath} -> {newPath}");
                        return true;
                    }
                    _logger.LogWarning($"File not found for renaming: {oldPath}");
                    return false;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to rename file: {oldPath} -> {newPath}");
                return false;
            }
        }

        public async Task<bool> BackupFileAsync(string source, string backupDirectory)
        {
            try
            {
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDirectory, $"{Path.GetFileNameWithoutExtension(source)}_backup_{timestamp}{Path.GetExtension(source)}");
                await CopyFileAsync(source, backupPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to backup file: {source}");
                return false;
            }
        }

        public async Task<bool> ValidateFileAsync(string filePath, string expectedHash)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found for validation: {filePath}");
                    return false;
                }

                using var md5 = MD5.Create();
                var fileHash = await ComputeFileHashAsync(filePath, md5);
                var hashString = BitConverter.ToString(fileHash).Replace("-", "").ToLower();

                return hashString == expectedHash.ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to validate file: {filePath}");
                return false;
            }
        }

        public void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInfo($"Deleted: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
            }
        }

        public void RenameFile(string source, string newName)
        {
            try
            {
                if (!File.Exists(source))
                {
                    _logger.LogWarning($"Source file not found: {source}");
                    return;
                }

                var directory = Path.GetDirectoryName(source);
                var newPath = Path.Combine(directory!, newName);
                File.Move(source, newPath);
                _logger.LogInfo($"Renamed file from {source} to {newPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error renaming file {source}");
            }
        }

        public void ZipFile(string source, string zipPath)
        {
            try
            {
                using var archive = System.IO.Compression.ZipFile.Open(zipPath, ZipArchiveMode.Create);
                archive.CreateEntryFromFile(source, Path.GetFileName(source));
                _logger.LogInfo($"Archived: {source} -> {zipPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error zipping file: {source}");
            }
        }

        public void UnzipFile(string zipPath, string extractTo)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractTo, overwriteFiles: true);
            _logger.LogInfo($"Unzipped: {zipPath} -> {extractTo}");
        }
    }
}
