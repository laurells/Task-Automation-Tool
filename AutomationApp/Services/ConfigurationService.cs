using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutomationApp.Models;
using AutomationApp.Interfaces;

namespace AutomationApp.Services
{
    public class ConfigurationService
    {
        private readonly ILoggerService _logger;

        public ConfigurationService(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AppConfiguration?> LoadConfigurationAsync(string configPath)
        {
            string? json = null;
            try
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning($"Configuration file not found: {fullPath}. Creating default configuration.");
                    var rulesConfigPath = Environment.GetEnvironmentVariable("RULES_CONFIG_PATH") ?? "config.rule.json";
                    var defaultConfig = new AppConfiguration(
                        rulesConfigPath,
                        new EmailConfiguration
                        {
                            SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "",
                            SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "", out var smtpPortParsed) ? smtpPortParsed : 587,
                            Email = Environment.GetEnvironmentVariable("EMAIL_ADDRESS") ?? "",
                            Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? "",
                            UseSmtpSsl = true,
                            ImapHost = Environment.GetEnvironmentVariable("IMAP_HOST") ?? "",
                            ImapPort = 993,
                            UseImapSsl = true
                        },
                        new LoggingConfiguration
                        {
                            LogDirectory = Environment.GetEnvironmentVariable("LOG_DIRECTORY") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"),
                            LogLevel = Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL"), true, out var level) ? level : LogLevel.Info,
                            EnableConsoleOutput = true,
                            EnableFileLogging = true,
                            EnableErrorLogging = true
                        }
                    );

                    json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(fullPath, json);
                    _logger.LogInfo($"Default configuration created at: {fullPath}");
                    return defaultConfig;
                }

                json = await File.ReadAllTextAsync(fullPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, options);

                if (config == null)
                {
                    _logger.LogError(null!, $"Failed to deserialize configuration: Result is null. JSON: {json}");
                    throw new JsonException("Failed to deserialize configuration");
                }

                if (string.IsNullOrEmpty(config.RulesConfigPath))
                    throw new InvalidOperationException("Rules configuration path is required");

                // Override with environment variables
                var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? config.Email.SmtpHost;
                var smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var smtpPortOverride) ? smtpPortOverride : config.Email.SmtpPort;
                var email = Environment.GetEnvironmentVariable("EMAIL_ADDRESS") ?? config.Email.Email;
                var password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? config.Email.Password;

                var updatedConfig = new AppConfiguration(
                    config.RulesConfigPath,
                    new EmailConfiguration
                    {
                        SmtpHost = smtpHost,
                        SmtpPort = smtpPort,
                        Email = email,
                        Password = password,
                        UseSmtpSsl = config.Email.UseSmtpSsl,
                        ImapHost = config.Email.ImapHost,
                        ImapPort = config.Email.ImapPort,
                        UseImapSsl = config.Email.UseImapSsl
                    },
                    config.Logging
                );

                json = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(fullPath, json);
                _logger.LogInfo($"Configuration updated with environment variables at: {fullPath}");
                return updatedConfig;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to deserialize configuration from {configPath}. JSON: {json}");
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Failed to read configuration file: {configPath}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error loading configuration: {configPath}");
                return null;
            }
        }
    }
}
