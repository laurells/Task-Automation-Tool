using System.Text.Json;
using AutomationApp.Services;
using AutomationApp.Models;

namespace AutomationApp.Utils
{
    public static class Helpers
    {
        public static EmailConfig LoadEmailConfig(Logger logger, string filePath = "emailsettings.json")
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
                if (!File.Exists(configPath))
                {
                    logger.LogWarning($"Email configuration file not found: {configPath}");
                    throw new FileNotFoundException($"Email configuration file not found: {configPath}");
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<EmailConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize EmailConfig from JSON.");

                return config;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load email configuration from {filePath}");
                throw;
            }
        }
    }
}