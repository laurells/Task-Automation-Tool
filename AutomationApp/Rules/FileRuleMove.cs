using AutomationApp.Core;
using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp.Rules
{
    public class FileMoveRule : IAutomationRule
    {
        public string Name { get; }
        private readonly string _source;
        private readonly string _target;
        private readonly FileService _fileService;
        private readonly Logger _logger;
        private readonly string[] _supportedExtensions;
        private readonly bool _addTimestamp;
        private readonly bool _backupFiles;

        public FileMoveRule(
            string source,
            string target,
            FileService fileService,
            Logger logger,
            string[] supportedExtensions = null!,
            bool addTimestamp = false,
            bool backupFiles = false,
            string name = "FileMoveRule")
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source directory cannot be null or empty.", nameof(source));
            if (string.IsNullOrEmpty(target))
                throw new ArgumentException("Target directory cannot be null or empty.", nameof(target));

            _source = source;
            _target = target;
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _supportedExtensions = supportedExtensions ?? Array.Empty<string>();
            _addTimestamp = addTimestamp;
            _backupFiles = backupFiles;
            Name = name;

            if (!Directory.Exists(_source))
                _logger.LogWarning($"Source directory does not exist: {_source}");
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
                }
            }
        }

        public string RuleName => Name; // Align with Name property

        public bool Enabled { get; set; } = true;

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                _logger.LogDebug($"Starting FileMoveRule execution with source: {_source}, target: {_target}");
                _logger.LogDebug($"Supported extensions: {string.Join(", ", _supportedExtensions)}");
                _logger.LogDebug($"Add timestamp: {_addTimestamp}, Backup files: {_backupFiles}");

                bool isSourceFile = File.Exists(_source);
                bool isTargetDirectory = Directory.Exists(_target);

                _logger.LogDebug($"Source is file: {isSourceFile}, Target is directory: {isTargetDirectory}");

                if (!isSourceFile && !Directory.Exists(_source))
                {
                    _logger.LogWarning($"Source path not found: {_source}");
                    return false;
                }

                if (!isTargetDirectory)
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

                int processedFiles = 0;
                int failedFiles = 0;

                if (isSourceFile)
                {
                    string sourceExt = Path.GetExtension(_source).ToLowerInvariant();
                    if (_supportedExtensions.Length == 0 || _supportedExtensions.Contains(sourceExt))
                    {
                        try
                        {
                            string dest = Path.Combine(_target, Path.GetFileName(_source));
                            if (File.Exists(dest))
                            {
                                if (await _fileService.CompareFileHashesAsync(_source, dest))
                                {
                                    _logger.LogInfo($"Skipping duplicate file: {Path.GetFileName(_source)}");
                                    return true;
                                }
                                if (_addTimestamp)
                                {
                                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    dest = Path.Combine(_target, $"{Path.GetFileNameWithoutExtension(_source)}_{timestamp}{sourceExt}");
                                }
                            }

                            if (_backupFiles)
                            {
                                var backupDest = Path.Combine("C:\\Backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                                Directory.CreateDirectory(backupDest);
                                var backupFile = Path.Combine(backupDest, Path.GetFileName(_source));
                                await _fileService.CopyFileAsync(_source, backupFile);
                                _logger.LogInfo($"Backed up file: {backupFile}");
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
                    foreach (var file in Directory.EnumerateFiles(_source))
                    {
                        string extension = Path.GetExtension(file).ToLowerInvariant();
                        if (_supportedExtensions.Length == 0 || _supportedExtensions.Contains(extension))
                        {
                            try
                            {
                                string dest = Path.Combine(_target, Path.GetFileName(file));
                                if (File.Exists(dest))
                                {
                                    if (await _fileService.CompareFileHashesAsync(file, dest))
                                    {
                                        _logger.LogInfo($"Skipping duplicate file: {Path.GetFileName(file)}");
                                        continue;
                                    }
                                    if (_addTimestamp)
                                    {
                                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                        dest = Path.Combine(_target, $"{Path.GetFileNameWithoutExtension(file)}_{timestamp}{extension}");
                                    }
                                }

                                if (_backupFiles)
                                {
                                    var backupDest = Path.Combine("C:\\Backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                                    Directory.CreateDirectory(backupDest);
                                    var backupFile = Path.Combine(backupDest, Path.GetFileName(file));
                                    await _fileService.CopyFileAsync(file, backupFile);
                                    _logger.LogInfo($"Backed up file: {backupFile}");
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
                var backupDir = Path.Combine("C:\\Backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
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