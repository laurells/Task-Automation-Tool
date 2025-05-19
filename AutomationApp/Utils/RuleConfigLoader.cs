using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutomationApp.Core;
using AutomationApp.Models;
using AutomationApp.Rules;
using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp.Utils
{
    /// <summary>
    /// Loads automation rules from a JSON configuration file and initializes an <see cref="AutomationEngine"/>.
    /// </summary>
    /// <remarks>
    /// Reads <c>config.rules.json</c> (or a specified file) to register rules like <see cref="FileMoveRule"/>,
    /// <see cref="BulkEmailRule"/>, and <see cref="DataProcessingRule"/> into an <see cref="AutomationEngine"/>.
    /// Integrates with <see cref="ILoggerService"/>, <see cref="IFileService"/>, <see cref="IDataService"/>,
    /// and <see cref="IEmailService"/> for rule execution.
    /// </remarks>
    public static class RuleConfigLoader
    {
        private static EmailConfiguration? _emailConfig;

        /// <summary>
        /// Asynchronously loads automation rules from a JSON file and initializes an <see cref="AutomationEngine"/>.
        /// </summary>
        /// <param name="loggingConfig">The logging configuration. Cannot be null.</param>
        /// <param name="filePath">The path to the rules configuration file. Defaults to "config.rules.json".</param>
        /// <param name="fileService">The file service for file operations. If null, a new instance is created.</param>
        /// <param name="dataService">The data service for data processing. If null, a new instance is created.</param>
        /// <param name="emailService">The email service for email rules. Cannot be null.</param>
        /// <returns>A task that resolves to the initialized <see cref="AutomationEngine"/> with registered rules.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggingConfig"/> or <paramref name="emailService"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the configuration file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when JSON deserialization fails.</exception>
        /// <exception cref="IOException">Thrown when reading the file fails.</exception>
        public static async Task<AutomationEngine> LoadRulesAsync(
            LoggingConfiguration loggingConfig,
            string filePath = "config.rules.json",
            IFileService? fileService = null,
            IDataService? dataService = null,
            IEmailService? emailService = null)
        {
            // Validate inputs
            if (loggingConfig == null)
                throw new ArgumentNullException(nameof(loggingConfig));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (emailService == null)
                throw new ArgumentNullException(nameof(emailService));

            // Initialize logger and engine
            var logger = new Logger("RuleConfigLoader", loggingConfig);
            var engine = new AutomationEngine(logger);

            // Initialize services
            fileService ??= new FileService(logger);
            dataService ??= new DataService(logger);

            // Load EmailConfig if not already loaded
            if (_emailConfig == null)
            {
                try
                {
                    var emailConfig = await Helpers.LoadEmailConfig(logger, "emailsettings.json");
                    _emailConfig = new EmailConfiguration
                    {
                        SmtpHost = emailConfig.SmtpHost ?? "smtp.example.com",
                        SmtpPort = emailConfig.SmtpPort,
                        UseSmtpSsl = emailConfig.UseSmtpSsl,
                        Email = emailConfig.Email ?? string.Empty,
                        Password = emailConfig.Password ?? string.Empty,
                        ImapHost = emailConfig.ImapHost ?? "imap.example.com",
                        ImapPort = emailConfig.ImapPort,
                        UseImapSsl = emailConfig.UseImapSsl
                    };
                    logger.LogInfo("Successfully loaded EmailConfiguration");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load EmailConfig. Skipping email-related rules.");
                    _emailConfig = null;
                }
            }

            try
            {
                // Resolve configuration file path
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
                logger.LogInfo($"Loading rules configuration from: {configPath}");
                if (!File.Exists(configPath))
                {
                    logger.LogWarning($"Rules configuration file not found: {configPath}");
                    return engine;
                }

                // Read JSON file asynchronously
                var json = await File.ReadAllTextAsync(configPath);
                logger.LogDebug($"Rules configuration content: {json}");

                // Deserialize JSON to rule configurations
                var ruleConfigs = JsonSerializer.Deserialize<List<RuleConfig>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (ruleConfigs == null || ruleConfigs.Count == 0)
                {
                    logger.LogWarning("No rules found in configuration file");
                    return engine;
                }

                // Track rule names to avoid duplicates
                var usedRuleNames = new HashSet<string>();
                int unnamedRuleCount = 0;

                // Process each rule configuration
                foreach (var config in ruleConfigs)
                {
                    try
                    {
                        // Validate rule type
                        if (string.IsNullOrEmpty(config.Type))
                        {
                            logger.LogWarning("Skipping rule with missing or invalid 'type'");
                            continue;
                        }

                        var type = config.Type.ToLowerInvariant();

                        // Generate rule name
                        var ruleName = !string.IsNullOrEmpty(config.Name) ? config.Name : type switch
                        {
                            "filemoverule" => "FileMoveRule",
                            "bulkemailrule" => "BulkEmailRule",
                            "dataprocessingrule" => "DataProcessingRule",
                            _ => "UnknownRule"
                        };

                        // Append suffix for unnamed rules to avoid duplicates
                        if (string.IsNullOrEmpty(config.Name))
                        {
                            unnamedRuleCount++;
                            ruleName = $"{ruleName}_{unnamedRuleCount}";
                        }

                        // Ensure unique rule name
                        while (usedRuleNames.Contains(ruleName))
                        {
                            ruleName = $"{ruleName}_{unnamedRuleCount++}";
                        }
                        usedRuleNames.Add(ruleName);

                        // Register rule based on type
                        switch (type)
                        {
                            case "filemoverule":
                                // Validate FileMoveRule properties
                                if (string.IsNullOrEmpty(config.Source))
                                {
                                    logger.LogWarning($"Skipping FileMoveRule '{ruleName}' with missing or invalid 'source'");
                                    continue;
                                }
                                if (string.IsNullOrEmpty(config.Target))
                                {
                                    logger.LogWarning($"Skipping FileMoveRule '{ruleName}' with missing or invalid 'target'");
                                    continue;
                                }

                                var extensions = config.SupportedExtensions ?? Array.Empty<string>();
                                var addTimestamp = config.AddTimestamp;
                                var backupFiles = config.BackupFiles;

                                engine.RegisterRule(new FileMoveRule(
                                    config.Source,
                                    config.Target,
                                    fileService,
                                    logger,
                                    extensions,
                                    addTimestamp,
                                    backupFiles,
                                    ruleName));
                                logger.LogInfo($"Registered FileMoveRule: {ruleName} (source: {config.Source}, target: {config.Target})");
                                break;

                            case "bulkemailrule":
                                // Validate BulkEmailRule properties
                                if (string.IsNullOrEmpty(config.CsvPath))
                                {
                                    logger.LogWarning($"Skipping BulkEmailRule '{ruleName}' with missing or invalid 'csvPath'");
                                    continue;
                                }
                                if (!File.Exists(config.CsvPath))
                                {
                                    logger.LogWarning($"CSV file not found for BulkEmailRule '{ruleName}': {config.CsvPath}");
                                    continue;
                                }

                                // Skip if email configuration is invalid
                                if (_emailConfig == null)
                                {
                                    logger.LogWarning($"Skipping BulkEmailRule '{ruleName}' due to invalid EmailConfiguration");
                                    continue;
                                }

                                engine.RegisterRule(new BulkEmailRule(
                                    emailService,
                                    dataService,
                                    _emailConfig,
                                    config.CsvPath,
                                    ruleName));
                                logger.LogInfo($"Registered BulkEmailRule: {ruleName} (csvPath: {config.CsvPath})");
                                break;

                            case "dataprocessingrule":
                                // Validate DataProcessingRule properties
                                var ruleFilePath = config.FilePath ?? config.Settings?.DataPath;
                                if (string.IsNullOrEmpty(ruleFilePath))
                                {
                                    logger.LogWarning($"Skipping DataProcessingRule '{ruleName}' with missing or invalid file path");
                                    continue;
                                }
                                if (!File.Exists(ruleFilePath))
                                {
                                    logger.LogWarning($"Data file not found for DataProcessingRule '{ruleName}': {ruleFilePath}");
                                    continue;
                                }
                                var requiredColumns = config.RequiredColumns ?? config.Settings?.RequiredColumns ?? Array.Empty<string>();

                                engine.RegisterRule(new DataProcessingRule(
                                    dataService,
                                    ruleFilePath,
                                    requiredColumns,
                                    logger,
                                    ruleName));
                                logger.LogInfo($"Registered DataProcessingRule: {ruleName} (filePath: {ruleFilePath})");
                                break;

                            default:
                                logger.LogWarning($"Skipping unknown rule type: {type}");
                                continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to register rule: {config.Type ?? "Unknown"}");
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, $"Failed to deserialize rules from {filePath}: Invalid JSON format");
                throw new InvalidOperationException($"Failed to deserialize rules from {filePath}: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, $"Failed to read rules configuration from {filePath}");
                throw new IOException($"Failed to read rules configuration from {filePath}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error loading rules from {filePath}");
                throw new InvalidOperationException($"Unexpected error loading rules from {filePath}: {ex.Message}", ex);
            }

            return engine;
        }
    }
}