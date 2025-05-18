using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutomationApp.Services;
using AutomationApp.Models;
using AutomationApp.Rules;
using AutomationApp.Cli;
using AutomationApp.Utils;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AutomationApp;
using ClosedXML;




var tempConfig = LoadConfiguration(new Logger("Bootstrap", new LoggingConfiguration()));
if (tempConfig == null)
{
    Console.WriteLine("Failed to load configuration. Exiting.");
    Console.ReadKey();
    return 1;
}

var logger = new Logger("Program", tempConfig.Logging);
try
{
    logger.LogInfo("Starting Task Automation Tool");

    var config = LoadConfiguration(logger);
    if (config == null)
    {
        logger.LogInfo("Failed to load configuration");
        return 1;
    }

    var engine = RuleConfigLoader.LoadRules(config.Logging);
    var fileService = new FileService(logger);
    var dataService = new DataService();
    var emailService = new EmailService(config.Email, logger);

    foreach (var rule in config.Rules)
    {
        try
        {
            if (!rule.Enabled)
            {
                logger.LogInfo($"Skipping disabled rule: {rule.Name}");
                continue;
            }

            Dictionary<string, object> settings = rule.Settings;
            switch (rule.Type.ToLower())
            {
                case "filemoverule":
                    if (!settings.TryGetValue("source", out object? sourceObj) || sourceObj is not string source || string.IsNullOrEmpty(source))
                    {
                        logger.LogInfo($"Missing or invalid 'source' for rule: {rule.Name}");
                        continue;
                    }
                    if (!settings.TryGetValue("target", out object? targetObj) || targetObj is not string target || string.IsNullOrEmpty(target))
                    {
                        logger.LogInfo($"Missing or invalid 'target' for rule: {rule.Name}");
                        continue;
                    }

                    var extensions = settings.TryGetValue("supportedExtensions", out object? extObj) && extObj is JsonElement extArray && extArray.ValueKind == JsonValueKind.Array
                        ? extArray.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToArray()!
                        : Array.Empty<string>();
                    var addTimestamp = settings.TryGetValue("addTimestamp", out object? tsObj) && tsObj is bool tsValue ? tsValue : false;
                    var backupFiles = settings.TryGetValue("backupFiles", out object? bfObj) && bfObj is bool bfValue ? bfValue : false;

                    engine.RegisterRule(new FileMoveRule(
                        source,
                        target,
                        fileService,
                        logger,
                        extensions as string[],
                        addTimestamp,
                        backupFiles));
                    logger.LogInfo($"Registered rule: {rule.Name}");
                    break;

                case "bulkemailrule":
                    var csvPath = settings.TryGetValue("csvPath", out object? csvObj) && csvObj is string csvValue ? csvValue : "recipients.csv";
                    if (!File.Exists(csvPath))
                    {
                        logger.LogWarning($"CSV file not found for rule {rule.Name}: {csvPath}");
                        continue;
                    }
                    engine.RegisterRule(new BulkEmailRule(
                        emailService,
                        dataService,
                        config.Email,
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

    if (args.Length > 0 && args[0].ToLower() == "gui")
    {
        // Launch Avalonia GUI
        var appBuilder = AppBuilder.Configure<App>()
            // .UsePlatformDetect()
            .LogToTrace();

        var app = appBuilder.StartWithClassicDesktopLifetime(args);
        if (app != 0)
        {
            logger.LogInfo($"Failed to start Avalonia application (error code: {app})");
            return 1;
        }

        // Create and show the main window
        var mainWindow = new MainWindow(engine, logger);
        mainWindow.Show();
        
        // Get the application instance and set the main window
        var appInstance = Avalonia.Application.Current as IClassicDesktopStyleApplicationLifetime;
        if (appInstance != null)
        {
            appInstance.MainWindow = mainWindow;
            appInstance.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        else
        {
            logger.LogInfo("Unsupported application lifetime");
        }
    }

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

AppConfiguration? LoadConfiguration(Logger logger)
{
    try
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
        {
            logger.LogWarning($"Configuration file not found: {configPath}. Creating default configuration.");
            var appConfig = new AppConfiguration
            {
                Email = new EmailConfiguration
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
                Rules = new List<AutomationRule>()
            };

            var jsonString = JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, jsonString);
            return appConfig;
        }

        var json = File.ReadAllText(configPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        var config = JsonSerializer.Deserialize<AppConfiguration>(json, options);

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
        logger.LogError(ex, $"Error loading configuration: {ex.Message}");
        return null;
    }
}


namespace AutomationApp
{

    // Required for Avalonia

    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}