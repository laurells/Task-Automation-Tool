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
    logger.Log("Starting Task Automation Tool");

    // Load configuration
    var config = LoadConfiguration();
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
                    engine.RegisterRule(new FileMoveRule(
                        rule.Settings["source"],
                        rule.Settings["target"],
                        fileService,
                        logger));
                    break;
                case "bulkemailrule":
                    engine.RegisterRule(new BulkEmailRule(
                        emailService,
                        dataService,
                        config
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
            config.Email.Host = "smtp.example.com";
            config.Email.Port = 587;
            config.Email.Username = "user@example.com";
            config.Email.Password = "password";
            config.Email.UseSsl = true;

            // Save default configuration
            var jsonString = JsonSerializer.Serialize(config);
            File.WriteAllText(configPath, jsonString);
            return config;
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<AppConfiguration>(json);
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
