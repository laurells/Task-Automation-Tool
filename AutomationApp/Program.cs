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
                LogDirectory = "Logs",
                LogLevel = LogLevel.Info,
                EnableConsoleOutput = true,
                EnableFileLogging = false,
                EnableErrorLogging = true
            });
        try
            {
            // Load configuration
            var configFile = Environment.GetEnvironmentVariable("CONFIG_FILE") ?? "appsettings.json";
            var configService = new ConfigurationService(bootstrapLogger);
            var config = await configService.LoadConfigurationAsync(configFile);
            if (config == null)
            {
                bootstrapLogger.LogError(null!, "Failed to load configuration. Exiting.");
                Console.WriteLine($"Failed to load configuration from {configFile}. Check console output.");
                return 1;
            }

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
            logger.LogInfo("Starting Task Automation Tool");

            // Verify rules config file
            var rulesConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.RulesConfigPath);
            if (!File.Exists(rulesConfigPath))
            {
                logger.LogWarning($"Rules configuration file not found: {rulesConfigPath}. Creating empty rules file.");
                await File.WriteAllTextAsync(rulesConfigPath, "[]");
            }

            // Load rules using RuleConfigLoader with DI services
            var engine = await RuleConfigLoader.LoadRulesAsync(
                    config.Logging,
                    config.RulesConfigPath,
                    provider.GetRequiredService<IFileService>(),
                    provider.GetRequiredService<IDataService>(),
                    provider.GetRequiredService<IEmailService>());
                logger.LogInfo("Rules loaded successfully");

                // Handle CLI or GUI mode based on arguments
                // Handle CLI or GUI
                if (args.Length == 0)
                {
                    logger.LogError(null!, "No command provided. Usage: dotnet run -- [gui|run|schedule|test|status|configure|list-rules|validate-rules]");
                    Console.WriteLine("No command provided. Usage: dotnet run -- [gui|run|schedule|test|status|configure|list-rules|validate-rules]");
                    return 1;
                }

                if (args[0].Equals("gui", StringComparison.OrdinalIgnoreCase))
                {
                    return RunGuiMode(args, engine, logger);
                }

                // CLI commands
                var commandHandler = new CommandHandler(engine, (Logger)logger);
                await commandHandler.HandleAsync(args);
                return 0;
            }
            catch (Exception ex)
            {
                bootstrapLogger.LogError(ex, "Fatal error in application startup");
                Console.WriteLine("An error occurred during application startup. Check console output.");
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
                var appBuilder = AppBuilder.Configure<App>()
                    // .UsePlatformDetect()
                    .LogToTrace();

                var app = appBuilder.StartWithClassicDesktopLifetime(args);
                if (app != 0)
                {
                    logger.LogError(null!, $"Failed to start Avalonia application (error code: {app})");
                    return 1;
                }

                var mainWindow = new MainWindow(engine, logger);
                mainWindow.Show();

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
        /// The Avalonia application class.
        /// </summary>
        /// <remarks>
        /// Initializes the Avalonia application and loads XAML resources for the GUI.
        /// </remarks>
        public class App : Application
        {
            public override void Initialize()
            {
                AvaloniaXamlLoader.Load(this);
            }
        }
    }
}