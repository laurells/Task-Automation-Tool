using AutomationApp.Interfaces;
using AutomationApp.Models;
using AutomationApp.Services;

namespace AutomationApp.Rules
{
    public class BulkEmailRule : IAutomationRule
    {
        private readonly EmailService _emailService;
        private readonly DataService _dataService;
        private readonly EmailConfiguration _config;
        private readonly string _csvPath;

        public BulkEmailRule(EmailService emailService, DataService dataService, EmailConfiguration config, string csvPath)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _csvPath = csvPath ?? throw new ArgumentNullException(nameof(csvPath));
        }

        public string RuleName => "EmailFromCSV";
        public bool Enabled { get; set; } = true;

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                var recipients = _dataService.ReadCsv<EmailRecipient>(_csvPath);
                if (recipients.Count == 0)
                {
                    Console.WriteLine($"No recipients loaded from {_csvPath}");
                    return false;
                }

                await _emailService.SendBulkEmailsAsync(_config, recipients);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing BulkEmailRule: {ex.Message}");
                return false;
            }
        }
    }
}