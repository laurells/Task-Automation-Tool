using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp.Rules
{
    /// <summary>
    /// Represents an automation rule for moving files from a source to a target directory.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IAutomationRule"/> to integrate with the automation engine.
    /// Supports moving individual files or all files in a directory, with options for timestamping, backups, and extension filtering.
    /// </remarks>
    public class FileMoveRule : IAutomationRule
    {
        private readonly string _source;                    // Source file or directory path
        private readonly string _target;                    // Target directory path
        private readonly IFileService _fileService;          // Service for file operations
        private readonly Logger _logger;                    // Logger for execution details
        private readonly string[] _supportedExtensions;     // Supported file extensions
        private readonly bool _addTimestamp;                // Whether to add timestamps to filenames
        private readonly bool _backupFiles;                 // Whether to back up files
        private string _ruleName;                           // Rule name

        /// <summary>
        /// Initializes a new instance of the <see cref="FileMoveRule"/> class.
        /// </summary>
        /// <param name="source">The source file or directory path. Cannot be null or empty.</param>
        /// <param name="target">The target directory path. Cannot be null or empty.</param>
        /// <param name="fileService">The service for file operations. Cannot be null.</param>
        /// <param name="logger">The logger for recording execution details. Cannot be null.</param>
        /// <param name="supportedExtensions">The supported file extensions (e.g., ".pdf"). Defaults to empty array.</param>
        /// <param name="addTimestamp">Whether to append timestamps to filenames to avoid overwrites. Defaults to false.</param>
        /// <param name="backupFiles">Whether to back up files before moving. Defaults to false.</param>
        /// <param name="name">The name of the rule. Defaults to "FileMoveRule".</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileService"/>, <paramref name="logger"/>, or <paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null or empty.</exception>
        public FileMoveRule(
            string source,
            string target,
            IFileService fileService,
            Logger logger,
            string[]? supportedExtensions = null,
            bool addTimestamp = false,
            bool backupFiles = false,
            string name = "FileMoveRule")
        {
            // Validate inputs
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source path cannot be null or empty.", nameof(source));
            if (string.IsNullOrEmpty(target))
                throw new ArgumentException("Target directory cannot be null or empty.", nameof(target));

            _source = source;
            _target = target;
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _supportedExtensions = supportedExtensions ?? Array.Empty<string>();
            _addTimestamp = addTimestamp;
            _backupFiles = backupFiles;
            _ruleName = name ?? throw new ArgumentNullException(nameof(name));

            // Validate source and target directories
            if (!Directory.Exists(_source) && !File.Exists(_source))
                _logger.LogWarning($"Source path does not exist: {_source}");
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

        /// <summary>
        /// Gets or sets the unique name of the rule.
        /// </summary>
        /// <remarks>Defaults to "FileMoveRule" if not specified in the constructor.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when setting to null.</exception>
        public string RuleName
        {
            get => _ruleName;
            set => _ruleName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled for execution.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Executes the file move rule by moving files from the source to the target directory.
        /// </summary>
        /// <param name="logger">The logger for recording execution details and errors. Cannot be null.</param>
        /// <returns>A task that resolves to true if all files were moved successfully; otherwise, false.</returns>
        /// <remarks>
        /// Moves a single file or all files in a directory, respecting supported extensions, timestamping, and backup options.
        /// Logs execution status and errors using the provided logger.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public async Task<bool> ExecuteAsync(Logger logger)
        {
            // Validate logger
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            // Check if rule is disabled
            if (!Enabled)
            {
                logger.LogInfo($"Rule '{RuleName}' is disabled. Skipping execution.");
                return true; // Disabled rules are considered successful
            }

            try
            {
                // Log execution start
                logger.LogDebug($"Starting FileMoveRule '{RuleName}' execution with source: {_source}, target: {_target}");
                logger.LogDebug($"Supported extensions: {string.Join(", ", _supportedExtensions)}");
                logger.LogDebug($"Add timestamp: {_addTimestamp}, Backup files: {_backupFiles}");

                bool isSourceFile = File.Exists(_source);
                bool isTargetDirectory = Directory.Exists(_target);

                logger.LogDebug($"Source is file: {isSourceFile}, Target is directory: {isTargetDirectory}");

                // Validate source
                if (!isSourceFile && !Directory.Exists(_source))
                {
                    logger.LogInfo($"Source path not found: {_source}");
                    return false;
                }

                // Ensure target directory exists
                if (!isTargetDirectory)
                {
                    try
                    {
                        Directory.CreateDirectory(_target);
                        logger.LogInfo($"Created target directory: {_target}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to create target directory: {_target}");
                        return false;
                    }
                }

                int processedFiles = 0;
                int failedFiles = 0;

                if (isSourceFile)
                {
                    // Process single file
                    if (await ProcessFileAsync(_source, _target, logger))
                        processedFiles++;
                    else
                        failedFiles++;
                }
                else
                {
                    // Process directory
                    logger.LogInfo($"Processing directory: {_source}");
                    foreach (var file in Directory.EnumerateFiles(_source))
                    {
                        if (await ProcessFileAsync(file, _target, logger))
                            processedFiles++;
                        else
                            failedFiles++;
                    }
                }

                // Log completion
                logger.LogSuccess($"FileMoveRule '{RuleName}' completed - Processed: {processedFiles}, Failed: {failedFiles}");
                return failedFiles == 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FileMoveRule '{RuleName}' execution");
                return false;
            }
        }

        /// <summary>
        /// Validates the rule’s configuration by checking source and target paths.
        /// </summary>
        /// <returns>A task that resolves to true if the configuration is valid; otherwise, false.</returns>
        /// <remarks>
        /// Ensures source and target paths are non-empty and creates the target directory if it doesn’t exist.
        /// Logs warnings or errors for invalid configurations.
        /// </remarks>
        public async Task<bool> ValidateConfiguration()
        {
            try
            {
                if (string.IsNullOrEmpty(_source) || string.IsNullOrEmpty(_target))
                {
                    _logger.LogWarning("Source or target path is empty");
                    return false;
                }

                if (!Directory.Exists(_source) && !File.Exists(_source))
                {
                    _logger.LogWarning($"Source path does not exist: {_source}");
                    return false;
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
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating FileMoveRule '{RuleName}' configuration");
                return false;
            }
        }

        /// <summary>
        /// Backs up files from the source to a timestamped backup directory.
        /// </summary>
        /// <returns>A task that resolves to true if all files were backed up successfully; otherwise, false.</returns>
        /// <remarks>
        /// Copies files to a backup directory under "C:\Backup" with a timestamped subdirectory.
        /// Logs backup status and errors.
        /// </remarks>
        public async Task<bool> BackupFiles()
        {
            try
            {
                var backupDir = Path.Combine("C:\\Backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);

                var files = Directory.Exists(_source) ? Directory.GetFiles(_source) : File.Exists(_source) ? new[] { _source } : Array.Empty<string>();
                foreach (var file in files)
                {
                    try
                    {
                        var dest = Path.Combine(backupDir, Path.GetFileName(file));
                        await _fileService.CopyFileAsync(file, dest);
                        _logger.LogInfo($"Backed up file: {Path.GetFileName(file)} to {dest}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to back up file: {Path.GetFileName(file)}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating backup for FileMoveRule '{RuleName}'");
                return false;
            }
        }

        /// <summary>
        /// Processes a single file by moving it to the target directory with optional timestamping and backup.
        /// </summary>
        /// <param name="sourceFile">The source file path.</param>
        /// <param name="targetDir">The target directory path.</param>
        /// <param name="logger">The logger for recording details.</param>
        /// <returns>A task that resolves to true if the file was processed successfully; otherwise, false.</returns>
        private async Task<bool> ProcessFileAsync(string sourceFile, string targetDir, Logger logger)
        {
            string extension = Path.GetExtension(sourceFile).ToLowerInvariant();
            if (_supportedExtensions.Length > 0 && !_supportedExtensions.Contains(extension))
            {
                logger.LogWarning($"File extension not supported: {extension} for file: {Path.GetFileName(sourceFile)}");
                return false;
            }

            try
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(sourceFile));
                if (File.Exists(dest))
                {
                    if (await _fileService.CompareFileHashesAsync(sourceFile, dest))
                    {
                        logger.LogInfo($"Skipping duplicate file: {Path.GetFileName(sourceFile)}");
                        return true;
                    }
                    if (_addTimestamp)
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        dest = Path.Combine(targetDir, $"{Path.GetFileNameWithoutExtension(sourceFile)}_{timestamp}{extension}");
                    }
                }

                if (_backupFiles)
                {
                    var backupDir = Path.Combine("C:\\Backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    Directory.CreateDirectory(backupDir);
                    var backupFile = Path.Combine(backupDir, Path.GetFileName(sourceFile));
                    await _fileService.CopyFileAsync(sourceFile, backupFile);
                    logger.LogInfo($"Backed up file: {Path.GetFileName(sourceFile)} to {backupFile}");
                }

                await _fileService.MoveFileAsync(sourceFile, dest);
                logger.LogInfo($"Successfully moved file: {Path.GetFileName(sourceFile)} to {dest}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to move file: {Path.GetFileName(sourceFile)}");
                return false;
            }
        }
    }
}