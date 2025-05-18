using AutomationApp.Services;
using System.Text.Json;
namespace AutomationApp.Utils;

using AutomationApp.Rules;
using AutomationApp.Core;

public static class RuleConfigLoader
{
    public static AutomationEngine LoadRules()
    {
        var logger = new Logger("RuleConfigLoader"); // Provide a category name for the Logger
        var engine = new AutomationEngine(logger);
        var config = Helpers.LoadEmailConfig();
        var json = File.ReadAllText("config.rules.json");
        var rawRules = JsonSerializer.Deserialize<List<JsonElement>>(json);

        var fileService = new FileService(logger);

        // Convert EmailConfig to EmailConfiguration if necessary
        var emailConfiguration = new AutomationApp.Models.EmailConfiguration
        {
            // Map properties from config (EmailConfig) to emailConfiguration (EmailConfiguration)
            // Example:
            // SmtpServer = config.SmtpServer,
            // Port = config.Port,
            // Username = config.Username,
            // Password = config.Password
        };

        var emailService = new EmailService(emailConfiguration, logger);
        var dataService = new DataService();

        if (rawRules != null)
        {
            foreach (var raw in rawRules)
            {
                var type = raw.GetProperty("type").GetString();
                if (type == "FileMoveRule")
                {
                    engine.RegisterRule(new FileMoveRule(
                        raw.GetProperty("source").GetString() ?? string.Empty,
                        raw.GetProperty("target").GetString() ?? string.Empty,
                        fileService,
                        logger
                        ));
                }
                else if (type == "BulkEmailRule")
                {
                    engine.RegisterRule(new BulkEmailRule(
                        emailService,
                        dataService,
                        config));
                }
            }
        }

        return engine;
    }
}
