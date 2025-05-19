using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AutomationApp.Cli;
using AutomationApp.Core;
using AutomationApp.Models;
using AutomationApp.Services;
using AutomationApp.Interfaces;
using AutomationApp.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AutomationApp
{
    /// <summary>
    /// The main entry point for the Task Automation Tool.
    /// </summary>
    /// <remarks>
    /// Initializes the application, loads configuration, and starts either CLI or GUI mode based on command-line arguments.
    /// Uses <see cref="Microsoft.Extensions.DependencyInjection"/> to manage services like <see cref="ILoggerService"/>,
    /// <see cref="IFileService"/>, <see cref="IDataService"/>, and <see cref="IEmailService"/>.
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">Command-line arguments (e.g., "gui" for GUI mode, "run" for CLI mode).</param>
        /// <returns>A task that resolves to the exit code (0 for success, 1 for failure).</returns>
        /// <remarks>
        /// Loads configuration from <c>appsettings.json</c>, sets up dependency injection, and delegates to
        /// <see cref="CommandHandler"/> for CLI mode or launches the Avalonia GUI with <see cref="MainWindow"/> for GUI mode.
        /// </remarks>
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            // Initialize bootstrap logger for configuration loading
            var bootstrapLogger = new Logger("Bootstrap", new LoggingConfiguration
            {
                LogDirectory = "C:\\Logs",
                LogLevel = LogLevel.Info,
                EnableConsoleOutput = true,
                EnableFileLogging = true,
                EnableErrorLogging = true
            });

            // Load configuration
            var config = await LoadConfigurationAsync(bootstrapLogger, "appsettings.json");
            if (config == null)
            {
                Console.WriteLine("Failed to load configuration. Exiting.");
                Console.ReadKey();
                return 1;
            }

            // write the logger for the DI container

            

            // Set up dependency injection
            var services = new ServiceCollection();
            
            
            // Register logger services
            var programLogger = new Logger("Program", config.Logging);
            services.AddSingleton<ILoggerService>(programLogger);
            services.AddSingleton(provider => programLogger);
            
            // Register other services with the logger
            services.AddSingleton<IFileService>(provider => new FileService(programLogger));
            services.AddSingleton<IDataService>(provider => new DataService(programLogger));
            services.AddSingleton<IEmailService>(provider => new EmailService(config.Email, programLogger));
            var provider = services.BuildServiceProvider();

            // Resolve logger for main program
            var logger = provider.GetRequiredService<ILoggerService>();
            // the logger should not be null, but we can check it
            if (logger == null)
            {
                Console.WriteLine("Logger service is not available. Exiting.");
                return 1;
            }
            try
            {
                logger.LogInfo("Starting Task Automation Tool");

                // Load rules using RuleConfigLoader with DI services
                var engine = await RuleConfigLoader.LoadRulesAsync(
                    config.Logging,
                    config.RulesConfigPath,
                    provider.GetRequiredService<IFileService>(),
                    provider.GetRequiredService<IDataService>(),
                    provider.GetRequiredService<IEmailService>());
                logger.LogInfo("Rules loaded successfully");

                // Handle CLI or GUI mode based on arguments
                if (args.Length == 0)
                {
                    Console.WriteLine("No command provided. Usage: dotnet run -- [gui|run]");
                    return 1;
                }

                if (args[0].Equals("gui", StringComparison.OrdinalIgnoreCase))
                {
                    return RunGuiMode(args, engine, logger);
                }

                if (args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInfo("Running CLI command");
                    var cliLogger = new Logger("CLI", config.Logging);
                    var commandHandler = new CommandHandler(engine, cliLogger);
                    await commandHandler.HandleAsync(args);
                    logger.LogInfo("CLI execution completed");
                    return 0;
                }

                Console.WriteLine($"Unknown command: {args[0]}. Usage: dotnet run -- [gui|run]");
                return 1;
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

        /// <summary>
        /// Runs the application in GUI mode using Avalonia.
        /// </summary>
        /// <param name="args">Command-line arguments for Avalonia.</param>
        /// <param name="engine">The automation engine for managing rules.</param>
        /// <param name="logger">The logger for GUI operations.</param>
        /// <returns>The exit code (0 for success, 1 for failure).</returns>
        /// <remarks>
        /// Configures Avalonia with platform-specific rendering, creates and displays <see cref="MainWindow"/>,
        /// and sets the application lifetime to close when the main window is closed.
        /// </remarks>
        private static int RunGuiMode(string[] args, AutomationEngine engine, ILoggerService logger)
        {
            try
            {
                // Configure and start Avalonia application
                var appBuilder = AppBuilder.Configure<App>()
                    //.UsePlatformDetect() // Enable platform-specific rendering
                    .LogToTrace();

                var app = appBuilder.StartWithClassicDesktopLifetime(args);
                if (app != 0)
                {
                    logger.LogError(null!, $"Failed to start Avalonia application (error code: {app})");
                    return 1;
                }

                // Create and show main window
                var mainWindow = new MainWindow(engine, logger);
                mainWindow.Show();

                // Set main window and shutdown mode
                if (Avalonia.Application.Current is IClassicDesktopStyleApplicationLifetime appInstance)
                {
                    appInstance.MainWindow = mainWindow;
                    appInstance.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    logger.LogInfo("Avalonia GUI started successfully");
                }
                else
                {
                    logger.LogError(null!, "Unsupported application lifetime");
                    return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start GUI mode");
                return 1;
            }
        }

        /// <summary>
        /// Asynchronously loads the application configuration from a JSON file.
        /// </summary>
        /// <param name="logger">The logger for configuration loading errors.</param>
        /// <param name="configPath">The path to the configuration file (default: "appsettings.json").</param>
        /// <returns>A task that resolves to the <see cref="AppConfiguration"/> or null if loading fails.</returns>
        /// <exception cref="JsonException">Thrown when JSON deserialization fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when required configuration properties are invalid.</exception>
        /// <remarks>
        /// Creates a default configuration if the file does not exist. Validates SMTP settings and rules config path.
        /// Uses case-insensitive JSON deserialization and supports enum string conversion.
        /// </remarks>
        private static async Task<AppConfiguration?> LoadConfigurationAsync(ILoggerService logger, string configPath = "appsettings.json")
        {
            try
            {
                // Resolve full path to configuration file
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
                if (!File.Exists(fullPath))
                {
                    logger.LogWarning($"Configuration file not found: {fullPath}. Creating default configuration.");
                    var defaultConfig = new AppConfiguration(
                        rulesConfigPath: "config.rules.json",
                        email: new EmailConfiguration
                        {
                            SmtpHost = "smtp.example.com",
                            SmtpPort = 587,
                            Email = "user@example.com",
                            Password = "password",
                            UseSmtpSsl = true,
                            ImapHost = "imap.example.com",
                            ImapPort = 993,
                            UseImapSsl = true
                        },
                        logging: new LoggingConfiguration
                        {
                            LogDirectory = "C:\\Logs",
                            LogLevel = LogLevel.Info,
                            EnableConsoleOutput = true,
                            EnableFileLogging = true,
                            EnableErrorLogging = true
                        });

                    // Serialize and write default configuration
                    var jsonString = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(fullPath, jsonString);
                    logger.LogInfo($"Default configuration created at: {fullPath}");
                    return defaultConfig;
                }

                // Read and deserialize configuration
                var json = await File.ReadAllTextAsync(fullPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, options);

                if (config == null)
                {
                    logger.LogError(null!, "Failed to deserialize configuration: Result is null");
                    throw new JsonException("Failed to deserialize configuration");
                }

                // Validate configuration properties
                if (string.IsNullOrEmpty(config.Email.SmtpHost))
                    throw new InvalidOperationException("SMTP host is required");
                if (config.Email.SmtpPort <= 0)
                    throw new InvalidOperationException("SMTP port must be greater than 0");
                if (string.IsNullOrEmpty(config.RulesConfigPath))
                    throw new InvalidOperationException("Rules configuration path is required");

                logger.LogInfo($"Configuration loaded from: {fullPath}");
                return config;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, $"Failed to deserialize configuration from {configPath}");
                return null;
            }
            catch (IOException ex)
            {
                logger.LogError(ex, $"Failed to read configuration file: {configPath}");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error loading configuration: {configPath}");
                return null;
            }
        }
    }

    /// <summary>
    /// The Avalonia application class.
    /// </summary>
    /// <remarks>
    /// Initializes the Avalonia application and loads XAML resources for the GUI.
    /// </remarks>
    public class App : Application
    {
        /// <summary>
        /// Initializes the application by loading XAML resources.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}