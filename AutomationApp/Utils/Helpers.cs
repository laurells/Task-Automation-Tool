using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutomationApp.Models;
using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp.Utils
{
    /// <summary>
    /// Provides utility methods for the automation framework, such as configuration loading.
    /// </summary>
    /// <remarks>
    /// Contains helper methods to load configuration files, integrating with <see cref="ILoggerService"/> for logging.
    /// Designed to support services like <see cref="EmailService"/> by loading <see cref="EmailConfig"/> from JSON files.
    /// </remarks>
    public static class Helpers
    {
        /// <summary>
        /// Asynchronously loads an email configuration from a JSON file.
        /// </summary>
        /// <param name="logger">The logger for recording operation details. Cannot be null.</param>
        /// <param name="filePath">The path to the JSON configuration file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to the deserialized <see cref="EmailConfig"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the configuration file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when JSON deserialization fails.</exception>
        /// <exception cref="IOException">Thrown when reading the file fails.</exception>
        /// <remarks>
        /// Reads a JSON file from the specified path, deserializes it into an <see cref="EmailConfig"/>, and logs errors using the provided <see cref="ILoggerService"/>.
        /// The file path is resolved relative to the application’s base directory.
        /// </remarks>
        public static async Task<EmailConfig> LoadEmailConfig(ILoggerService logger, string filePath)
        {
            // Validate inputs
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            try
            {
                // Resolve full path relative to base directory
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
                if (!File.Exists(configPath))
                {
                    logger.LogWarning($"Email configuration file not found: {configPath}");
                    throw new FileNotFoundException($"Email configuration file not found: {configPath}");
                }

                // Read JSON file asynchronously
                var json = await File.ReadAllTextAsync(configPath);

                // Deserialize JSON to EmailConfig
                var config = JsonSerializer.Deserialize<EmailConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    logger.LogError(null!, $"Failed to deserialize EmailConfig from {configPath}: Deserialization returned null.");
                    throw new InvalidOperationException($"Failed to deserialize EmailConfig from {configPath}: Deserialization returned null.");
                }

                if (string.IsNullOrEmpty(config.Email))
                {
                    logger.LogError(null!, $"Invalid EmailConfig: Email property is missing.");
                    throw new InvalidOperationException("Invalid EmailConfig: Email property is missing.");
                }

                logger.LogInfo($"Successfully loaded email configuration from {configPath}");
                return config;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, $"Failed to deserialize email configuration from {filePath}: Invalid JSON format.");
                throw new InvalidOperationException($"Failed to deserialize email configuration from {filePath}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load email configuration from {filePath}");
                throw new IOException($"Failed to load email configuration from {filePath}: {ex.Message}", ex);
            }
        }
    }
}