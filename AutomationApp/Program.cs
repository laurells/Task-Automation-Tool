using System;
using System.IO;
using System.Text.Json;
using AutomationApp.Services;
using AutomationApp.Models;
using AutomationApp.Rules;
using AutomationApp.Cli;
using AutomationApp.Core;

namespace AutomationApp
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var logger = new Logger("Program");
            try
            {
                logger.LogInfo("Starting Task Automation Tool");

                // Load configuration
                var config = LoadConfiguration();
                if (config == null)
                {
                    logger.LogError(new Exception("Failed to load configuration"));
                    return 1;
                }

                // Initialize services
                var fileService = new FileService(logger);
                var dataService = new DataService();
                var emailService = new EmailService(config.Email, logger);

                // Initialize engine
                var engine = new AutomationEngine(logger);

                // Register rules from configuration
                foreach (var rule in config.Rules)
                {
                    try
                    {
                        if (!rule.Enabled)
                        {
                            logger.LogInfo($"Skipping disabled rule: {rule.Name}");
                            continue;
                        }

                        switch (rule.Type.ToLower())
                        {
                            case "filemoverule":
                                var settings = rule.Settings;
                                if (!settings.TryGetValue("source", out string? source) || string.IsNullOrEmpty(source))
                                {
                                    logger.LogError($"Missing or invalid 'source' for rule: {rule.Name}");
                                    continue;
                                }
                                if (!settings.TryGetValue("target", out string? target) || string.IsNullOrEmpty(target))
                                {
                                    logger.LogError($"Missing or invalid 'target' for rule: {rule.Name}");
                                    continue;
                                }

                                var extensions = settings.TryGetValue("supportedExtensions", out string? extStr) && !string.IsNullOrWhiteSpace(extStr)
                                    ? extStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    : Array.Empty<string>();
                                var addTimestamp = bool.Parse(settings.GetValueOrDefault("addTimestamp", "false"));
                                var backupFiles = bool.Parse(settings.GetValueOrDefault("backupFiles", "false"));

                                engine.RegisterRule(new FileMoveRule(
                                    source,
                                    target,
                                    fileService,
                                    logger,
                                    extensions,
                                    addTimestamp,
                                    backupFiles));
                                logger.LogInfo($"Registered rule: {rule.Name}");
                                break;

                            case "bulkemailrule":
                                var csvPath = settings.GetValueOrDefault("csvPath", "recipients.csv");
                                if (!File.Exists(csvPath))
                                {
                                    logger.LogWarning($"CSV file not found for rule {rule.Name}: {csvPath}");
                                    continue;
                                }
                                engine.RegisterRule(new BulkEmailRule(
                                    emailService,
                                    dataService,
                                    config.Email, // Use config.Email instead of new EmailConfig()
                                    csvPath));
                                logger.LogInfo($"Registered rule: {rule.Name}");
                                break;

                            default:
                                logger.LogWarning($"Unknown rule type: {rule.Type}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to register rule: {rule.Name}");
                    }
                }

                // Handle CLI commands
                var commandHandler = new CommandHandler(engine, logger);
                await commandHandler.HandleAsync(args);
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error in application startup");
                Console.WriteLine("An error occurred during application startup. Please check the logs for more details.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return 1;
            }
        }

        static AppConfiguration? LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (!File.Exists(configPath))
                {
                    var config = new AppConfiguration
                    {
                        Email = new EmailConfig
                        {
                            SmtpHost = "smtp.example.com",
                            SmtpPort = 587,
                            Email = "user@example.com",
                            Password = "password",
                            UseSmtpSsl = true
                        },
                        Rules = new List<AutomationRule>()
                    };

                    var jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(configPath, jsonString);
                    return config;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(json);

                if (config == null)
                {
                    throw new JsonException("Failed to deserialize configuration");
                }

                if (string.IsNullOrEmpty(config.Email.SmtpHost))
                    throw new InvalidOperationException("SMTP host is required");
                if (config.Email.SmtpPort <= 0)
                    throw new InvalidOperationException("SMTP port must be greater than 0");
                config.Rules ??= new List<AutomationRule>();

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return null;
            }
        }
    }
}