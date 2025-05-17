using AutomationApp.Interfaces;
using AutomationApp.Services;
using Microsoft.Extensions.Logging;

namespace AutomationApp.Rules
{
    public class FileMoveRule : IAutomationRule
    {
        private readonly string _source;
        private readonly string _target;
        private readonly FileService _fileService;
        private readonly ILogger _logger;
        private readonly string[] _supportedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".json", ".xml" };

        public FileMoveRule(string source, string target, FileService fileService, ILogger logger) 
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("Source and target paths cannot be null or empty.");
            }

            if (!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {source}");
            }

            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            _source = source;
            _target = target;
            _fileService = fileService;
            _logger = logger;
        }


        public string RuleName => $"MoveFilesTo_{Path.GetFileName(_target)}";

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                if (!Directory.Exists(_source))
                {
                    _logger.LogWarning($"Source directory not found: {_source}");
                    return false;
                }

                if (!Directory.Exists(_target))
                {
                    try
                    {
                        Directory.CreateDirectory(_target);
                        _logger.LogInformation($"Created target directory: {_target}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to create target directory: {_target}");
                        return false;
                    }
                }

                var processedFiles = 0;
                var failedFiles = 0;

                foreach (var extension in _supportedExtensions)
                {
                    var files = Directory.GetFiles(_source, $"*{extension}");
                    foreach (var file in files)
                    {
                        try
                        {
                            var dest = Path.Combine(_target, Path.GetFileName(file));
                            
                            // Check if file already exists in target
                            if (File.Exists(dest))
                            {
                                // Compare file hashes to avoid duplicates
                                if (await _fileService.CompareFileHashesAsync(file, dest))
                                {
                                    _logger.LogInformation($"Skipping duplicate file: {Path.GetFileName(file)}");
                                    continue;
                                }
                                
                                // Add timestamp to avoid overwriting
                                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                dest = Path.Combine(_target, $"{Path.GetFileNameWithoutExtension(file)}_{timestamp}{Path.GetExtension(file)}");
                            }
                            else
                            {
                                // Add timestamp to avoid overwriting
                                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                dest = Path.Combine(_target, $"{Path.GetFileNameWithoutExtension(file)}_{timestamp}{Path.GetExtension(file)}");
                            }

                            await _fileService.MoveFileAsync(file, dest);
                            processedFiles++;
                            _logger.LogInformation($"Moved file: {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            failedFiles++;
                            _logger.LogError(ex, $"Failed to move file: {Path.GetFileName(file)}");
                        }
                    }
                }

                _logger.LogInformation($"File processing completed - Processed: {processedFiles}, Failed: {failedFiles}");
                return failedFiles == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FileMoveRule execution");
                return false;
            }
        }
            }
        }

        public Task<bool> ValidateConfiguration()
        {
            try
            {
                if (string.IsNullOrEmpty(_source) || string.IsNullOrEmpty(_target))
                {
                    _logger.LogError("Source or target directory path is empty");
                    return Task.FromResult(false);
                }

                if (!Directory.Exists(_source))
                {
                    _logger.LogError($"Source directory does not exist: {_source}");
                    return Task.FromResult(false);
                }

                if (!Directory.Exists(_target))
                {
                    try
                    {
                        Directory.CreateDirectory(_target);
                        _logger.LogInformation($"Created target directory: {_target}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to create target directory: {_target}");
                        return Task.FromResult(false);
                    }
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file move rule configuration");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> BackupFiles()
        {
            try
            {
                var backupDir = Path.Combine(_target, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);

                var files = Directory.GetFiles(_source);
                foreach (var file in files)
                {
                    try
                    {
                        var dest = Path.Combine(backupDir, Path.GetFileName(file));
                        await _fileService.CopyFileAsync(file, dest);
                        _logger.LogInformation($"Backed up file: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to backup file: {Path.GetFileName(file)}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return false;
            }
        }
    }
}
