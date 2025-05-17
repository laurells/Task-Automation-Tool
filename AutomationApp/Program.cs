using AutomationApp.Services;
using AutomationApp.Models;
using AutomationApp.Interfaces;
using System.Text.Json;
using System.IO;
using AutomationApp.Rules;
using AutomationApp.Cli;

try
{
    // Initialize logger
    var logger = new Logger("Program");
    Console.WriteLine("Starting Task Automation Tool");

    // Load configuration
    var config = LoadConfiguration();
    var emailConfig = new EmailConfig();
    if (config == null)
    {
        logger.LogError(new Exception("Failed to load configuration"));
        return 1;
    }

    // Initialize services with proper dependency injection
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
            if (!rule.Enabled) continue;

            switch (rule.Type.ToLower())
            {
                case "filemoverule":
                    var settings = rule.Settings;
                    var extensions = (settings.ContainsKey("supportedExtensions") && settings["supportedExtensions"] is string extStr && !string.IsNullOrWhiteSpace(extStr))
                        ? extStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : Array.Empty<string>();
                    var addTimestamp = bool.Parse(settings["addTimestamp"] ?? "false");
                    var backupFiles = bool.Parse(settings["backupFiles"] ?? "false");
                    
                    engine.RegisterRule(new FileMoveRule(
                        settings["source"],
                        settings["target"],
                        fileService,
                        logger,
                        extensions,
                        addTimestamp,
                        backupFiles));
                    break;
                case "bulkemailrule":
                    engine.RegisterRule(new BulkEmailRule(
                        emailService,
                        dataService,
                        emailConfig
                    ));
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
    await CommandHandler.HandleAsync(args, engine, logger);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("Fatal error in application startup");
    return 1;
}

static AppConfiguration? LoadConfiguration()
{
    try
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        
        if (!File.Exists(configPath))
        {
            // Create default configuration
            var config = new AppConfiguration();
            config.Email.SmtpHost = "smtp.example.com";
            config.Email.SmtpPort = 587;
            config.Email.Email = "user@example.com";
            config.Email.Password = "password";
            config.Email.UseSmtpSsl = true;

            // Save default configuration
            var jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, jsonString);
            return config;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json);
            
            if (config == null)
            {
                throw new JsonException("Failed to deserialize configuration");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(config.Email.SmtpHost))
                throw new InvalidOperationException("SMTP host is required");
            if (config.Email.SmtpPort <= 0)
                throw new InvalidOperationException("SMTP port must be greater than 0");
            config.Rules ??= new List<AutomationRule>();

            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading configuration: {ex.Message}");
        return null;
    }
}


// var config = Helpers.LoadEmailConfig();
// var emailService = new EmailService();
// var fileService = new FileService();
// var fileWatcher = new FileWatcherService(fileService);

// await EmailService.ApplyInboxRulesAsync(config);

// string watchFolder = @"C:\Watch";
// string destinationFolder = @"C:\Sorted\PDFs";
// fileWatcher.StartWatching(watchFolder, "pdf", destinationFolder);
// Console.WriteLine("Press 'q' to quit the sample.");
// while (Console.Read() != 'q') ;
// fileWatcher.StopWatching();
// if (string.IsNullOrEmpty(config.SmtpHost))
// {
//     throw new ArgumentNullException(nameof(config.SmtpHost), "SMTP host cannot be null or empty.");
// }
// if (string.IsNullOrEmpty(config.Email))
// {
//     throw new ArgumentNullException(nameof(config.Email), "Email cannot be null or empty.");
// }
// if (string.IsNullOrEmpty(config.Password))
// {
//     throw new ArgumentNullException(nameof(config.Password), "Password cannot be null or empty.");
// }
// await EmailService.SendEmailAsync(
// 	config.SmtpHost,
// 	config.SmtpPort,
// 	config.UseSmtpSsl,
// 	config.Email,
// 	config.Password,
// 	$"File Watcher Status - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
// 	"File Watcher Status",
// 	"File watcher has stopped."
// );
// Console.WriteLine("File watcher has stopped.");
// if (string.IsNullOrEmpty(config.ImapHost))
// {
// 	throw new ArgumentNullException(nameof(config.ImapHost), "IMAP host cannot be null or empty.");
// }
// await EmailService.ReadInboxAsync(config.ImapHost, config.ImapPort, config.UseImapSsl, config.Email, config.Password);
// Console.WriteLine("Press any key to exit.");    
// Console.ReadKey();
// // End of file
