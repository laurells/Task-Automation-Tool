using AutomationApp.Interfaces;
using AutomationApp.Services;

namespace AutomationApp.Rules
{
    public class FileMoveRule : IAutomationRule
    {
        private readonly string _source;
        private readonly string _target;
        private readonly FileService _fileService;
        private readonly Logger _logger;
        private readonly string[] _supportedExtensions;
        private readonly bool _addTimestamp;
        private readonly bool _backupFiles;

        public FileMoveRule(string source, string target, FileService fileService, Logger logger, 
            string[] supportedExtensions = null, bool addTimestamp = true, bool backupFiles = false) 
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
            _supportedExtensions = supportedExtensions ?? [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".json", ".xml"];
            _addTimestamp = addTimestamp;
            _backupFiles = backupFiles;
        }


        public string RuleName => $"MoveFilesTo_{Path.GetFileName(_target)}";

        public bool Enabled { get; set; } = true;

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                _logger.LogDebug($"Starting FileMoveRule execution with source: {_source}, target: {_target}");
                _logger.LogDebug($"Supported extensions: {string.Join(", ", _supportedExtensions)}");
                _logger.LogDebug($"Add timestamp: {_addTimestamp}, Backup files: {_backupFiles}");
                
                // Check if source is a file or directory
                bool isSourceFile = File.Exists(_source);
                bool isTargetDirectory = Directory.Exists(_target);

                _logger.LogDebug($"Source is file: {isSourceFile}, Target is directory: {isTargetDirectory}");

                if (!isSourceFile && !Directory.Exists(_source))
                {
                    _logger.LogWarning($"Source path not found: {_source}");
                    return false;
                }

                if (!isTargetDirectory && !Directory.Exists(_target))
                {
                    try
                    {
                        Directory.CreateDirectory(_target);
                        _logger.LogInfo($"Created target directory: {_target}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to create target directory: {_target}");
                        return false;
                    }
                }

                var processedFiles = 0;
                var failedFiles = 0;

                if (isSourceFile)
                {
                    // Handle single file move
                    string sourceExt = Path.GetExtension(_source).ToLower();
                    if (_supportedExtensions.Contains(sourceExt))
                    {
                        try
                        {
                            string dest = Path.Combine(_target, Path.GetFileName(_source));
                            _logger.LogDebug($"Attempting to move file: {_source} to {dest}");
                            
                            // Check if file already exists in target
                            if (File.Exists(dest))
                            {
                                _logger.LogDebug($"Destination file exists: {dest}");
                                if (await _fileService.CompareFileHashesAsync(_source, dest))
                                {
                                    _logger.LogInfo($"Skipping duplicate file: {Path.GetFileName(_source)}");
                                    return true;
                                }
                                
                                if (_addTimestamp)
                                {
                                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    dest = Path.Combine(_target, $"{Path.GetFileNameWithoutExtension(_source)}_{timestamp}{Path.GetExtension(_source)}");
                                    _logger.LogDebug($"Using timestamped destination: {dest}");
                                }
                            }

                            if (_backupFiles)
                            {
                                var backupDest = Path.Combine(_target, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                                Directory.CreateDirectory(backupDest);
                                var backupFile = Path.Combine(backupDest, Path.GetFileName(_source));
                                _logger.LogDebug($"Creating backup of file: {_source} to {backupFile}");
                                await _fileService.CopyFileAsync(_source, backupFile);
                            }

                            await _fileService.MoveFileAsync(_source, dest);
                            processedFiles++;
                            _logger.LogInfo($"Successfully moved file: {Path.GetFileName(_source)}");
                        }
                        catch (Exception ex)
                        {
                            failedFiles++;
                            _logger.LogError(ex, $"Failed to move file: {Path.GetFileName(_source)}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"File extension not supported: {sourceExt}");
                    }
                }
                else
                {
                    _logger.LogInfo($"Processing directory: {_source}");
                    // Handle directory move
                    foreach (var extension in _supportedExtensions)
                    {
                        _logger.LogDebug($"Searching for files with extension: {extension}");
                        var files = Directory.GetFiles(_source, $"*{extension}");
                        _logger.LogDebug($"Found {files.Length} files with extension {extension}");
                        
                        foreach (var file in files)
                        {
                            try
                            {
                                string dest = Path.Combine(_target, Path.GetFileName(file));
                                _logger.LogDebug($"Attempting to move file: {file} to {dest}");
                                
                                // Check if file already exists in target
                                if (File.Exists(dest))
                                {
                                    _logger.LogDebug($"Destination file exists: {dest}");
                                    if (await _fileService.CompareFileHashesAsync(file, dest))
                                    {
                                        _logger.LogInfo($"Skipping duplicate file: {Path.GetFileName(file)}");
                                        continue;
                                    }
                                    
                                    if (_addTimestamp)
                                    {
                                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                        dest = Path.Combine(_target, $"{Path.GetFileNameWithoutExtension(file)}_{timestamp}{Path.GetExtension(file)}");
                                        _logger.LogDebug($"Using timestamped destination: {dest}");
                                    }
                                }

                                if (_backupFiles)
                                {
                                    var backupDest = Path.Combine(_target, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                                    Directory.CreateDirectory(backupDest);
                                    var backupFile = Path.Combine(backupDest, Path.GetFileName(file));
                                    _logger.LogDebug($"Creating backup of file: {file} to {backupFile}");
                                    await _fileService.CopyFileAsync(file, backupFile);
                                }

                                await _fileService.MoveFileAsync(file, dest);
                                processedFiles++;
                                _logger.LogInfo($"Successfully moved file: {Path.GetFileName(file)}");
                            }
                            catch (Exception ex)
                            {
                                failedFiles++;
                                _logger.LogError(ex, $"Failed to move file: {Path.GetFileName(file)}");
                            }
                        }
                    }
                }

                _logger.LogInfo($"File processing completed - Processed: {processedFiles}, Failed: {failedFiles}");
                return failedFiles == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FileMoveRule execution");
                return false;
            }
        }

        public Task<bool> ValidateConfiguration()
        {
            try
            {
                if (string.IsNullOrEmpty(_source) || string.IsNullOrEmpty(_target))
                {
                    _logger.LogWarning("Source or target directory path is empty");
                    return Task.FromResult(false);
    
                }

                if (!Directory.Exists(_source))
                {
                    _logger.LogWarning($"Source directory does not exist: {_source}");
                    return Task.FromResult(false);
                }

                if (!Directory.Exists(_target))
                {
                    try
                    {
                        Directory.CreateDirectory(_target);
                        _logger.LogInfo($"Created target directory: {_target}");
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
                        _logger.LogInfo($"Backed up file: {Path.GetFileName(file)}");
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
