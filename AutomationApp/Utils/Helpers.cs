using System.Text.Json;
using AutomationApp.Models;

namespace AutomationApp.Utils
{
    public static class Helpers
    {
        public static EmailConfig LoadEmailConfig(string filePath = "emailsettings.json")
        {
            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<EmailConfig>(json) ?? throw new InvalidOperationException("Failed to deserialize EmailConfig from JSON.");
            return config;
        }
    }
}
