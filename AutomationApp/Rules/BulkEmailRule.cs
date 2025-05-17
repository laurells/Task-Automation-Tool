using AutomationApp.Interfaces;
using AutomationApp.Models;
using AutomationApp.Services;

public class BulkEmailRule(EmailService emailService, DataService dataService, EmailConfig config) : IAutomationRule
{
    public string RuleName => "EmailFromCSV";
    private readonly EmailService _emailService = emailService;
    private readonly DataService _dataService = dataService;
    private readonly EmailConfig _config = config;

    public async Task ExecuteAsync()
    {
        var recipients = DataService.ReadCsv<EmailRecipient>("recipients.csv");
        await _emailService.SendBulkEmailsAsync(_config, recipients);
    }
}
