using AutomationApp.Services;
using System.Text.Json;
namespace AutomationApp.Utils;

public static class RuleConfigLoader
{
    public static AutomationEngine LoadRules()
    {
        var engine = new AutomationEngine();
        var config = Helpers.LoadEmailConfig();
        var json = File.ReadAllText("config.rules.json");
        var rawRules = JsonSerializer.Deserialize<List<JsonElement>>(json);

        var fileService = new FileService();
        var emailService = new EmailService();
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
                        fileService));
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
