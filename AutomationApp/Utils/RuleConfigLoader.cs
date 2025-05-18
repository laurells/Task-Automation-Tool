using System.Text.Json;
using AutomationApp.Services;
using AutomationApp.Rules;
using AutomationApp.Core;
using AutomationApp.Models;

namespace AutomationApp.Utils
{
    public static class RuleConfigLoader
    {
        public static AutomationEngine LoadRules(LoggingConfiguration loggingConfig)
        {
            var logger = new Logger("RuleConfigLoader", loggingConfig);
            var engine = new AutomationEngine(logger);
            var fileService = new FileService(logger);
            var dataService = new DataService();

            // Load EmailConfig
            EmailConfig config;
            try
            {
                config = Helpers.LoadEmailConfig(logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load EmailConfig. Skipping email-related rules.");
                config = new EmailConfig();
            }

            var emailConfiguration = new AutomationApp.Models.EmailConfiguration
            {
                SmtpHost = config.SmtpHost ?? "smtp.example.com",
                SmtpPort = config.SmtpPort,
                UseSmtpSsl = config.UseSmtpSsl,
                Email = config.Email ?? string.Empty,
                Password = config.Password ?? string.Empty,
                ImapHost = config.ImapHost ?? "imap.example.com",
                ImapPort = config.ImapPort,
                UseImapSsl = config.UseImapSsl
            };

            bool isEmailConfigValid = !string.IsNullOrEmpty(emailConfiguration.SmtpHost) &&
                                     !string.IsNullOrEmpty(emailConfiguration.Email) &&
                                     !string.IsNullOrEmpty(emailConfiguration.Password);

            var emailService = new EmailService(emailConfiguration, logger);

            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.rules.json");
                if (!File.Exists(configPath))
                {
                    logger.LogWarning($"Rules configuration file not found: {configPath}");
                    return engine;
                }

                var json = File.ReadAllText(configPath);
                var rawRules = JsonSerializer.Deserialize<List<JsonElement>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (rawRules == null || rawRules.Count == 0)
                {
                    logger.LogWarning("No rules found in config.rules.json");
                    return engine;
                }

                foreach (var raw in rawRules)
                {
                    try
                    {
                        if (!raw.TryGetProperty("type", out var typeElement) || string.IsNullOrEmpty(typeElement.GetString()))
                        {
                            logger.LogWarning("Skipping rule with missing or invalid 'type'");
                            continue;
                        }

                        var type = typeElement.GetString()?.ToLower();
                        var ruleName = raw.TryGetProperty("name", out var nameElement) && !string.IsNullOrEmpty(nameElement.GetString())
                            ? nameElement.GetString()!
                            : type switch
                            {
                                "filemoverule" => "FileMoveRule",
                                "bulkemailrule" => "BulkEmailRule",
                                _ => "UnknownRule"
                            };

                        switch (type)
                        {
                            case "filemoverule":
                                if (!raw.TryGetProperty("source", out var sourceElement) || string.IsNullOrEmpty(sourceElement.GetString()))
                                {
                                    logger.LogWarning("Skipping FileMoveRule with missing or invalid 'source'");
                                    continue;
                                }
                                if (!raw.TryGetProperty("target", out var targetElement) || string.IsNullOrEmpty(targetElement.GetString()))
                                {
                                    logger.LogWarning("Skipping FileMoveRule with missing or invalid 'target'");
                                    continue;
                                }

                                var source = sourceElement.GetString()!;
                                var target = targetElement.GetString()!;
                                var extensions = raw.TryGetProperty("supportedExtensions", out var extElement) && extElement.ValueKind == JsonValueKind.Array
                                    ? extElement.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToArray()!
                                    : Array.Empty<string>();
                                var addTimestamp = raw.TryGetProperty("addTimestamp", out var tsElement) && tsElement.ValueKind == JsonValueKind.True;
                                var backupFiles = raw.TryGetProperty("backupFiles", out var bfElement) && bfElement.ValueKind == JsonValueKind.True;

                                engine.RegisterRule(new FileMoveRule(
                                    source,
                                    target,
                                    fileService,
                                    logger,
                                    extensions as string[],
                                    addTimestamp,
                                    backupFiles,
                                    ruleName));
                                logger.LogInfo($"Registered rule: {ruleName} (source: {source})");
                                break;

                            case "bulkemailrule":
                                if (!isEmailConfigValid)
                                {
                                    logger.LogWarning("Skipping BulkEmailRule due to invalid EmailConfiguration");
                                    continue;
                                }
                                if (!raw.TryGetProperty("csvPath", out var csvElement) || string.IsNullOrEmpty(csvElement.GetString()))
                                {
                                    logger.LogWarning("Skipping BulkEmailRule with missing or invalid 'csvPath'");
                                    continue;
                                }

                                var csvPath = csvElement.GetString()!;
                                if (!File.Exists(csvPath))
                                {
                                    logger.LogWarning($"CSV file not found for BulkEmailRule: {csvPath}");
                                    continue;
                                }

                                engine.RegisterRule(new BulkEmailRule(
                                    emailService,
                                    dataService,
                                    emailConfiguration,
                                    csvPath,
                                    ruleName)); // Updated to pass ruleName
                                logger.LogInfo($"Registered rule: {ruleName} (csv: {csvPath})");
                                break;

                            default:
                                logger.LogWarning($"Unknown rule type: {type}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to register rule: {raw.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load rules from config.rules.json");
            }

            return engine;
        }
    }
}