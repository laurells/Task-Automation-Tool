using AutomationApp.Interfaces;
using AutomationApp.Models;
using AutomationApp.Services;

public class BulkEmailRule(EmailService emailService, DataService dataService, EmailConfig config) : IAutomationRule
{
    public string RuleName => "EmailFromCSV";
    public bool Enabled { get; set; } = true;
    private readonly EmailService _emailService = emailService;
    private readonly DataService _dataService = dataService;
    private readonly EmailConfig _config = config;

    public async Task<bool> ExecuteAsync()
    {
        var recipients = DataService.ReadCsv<EmailRecipient>("recipients.csv");
        await _emailService.SendBulkEmailsAsync(_config, recipients);
        return true;
    }
}
