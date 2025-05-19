using AutomationApp.Core;
using AutomationApp.Services;
using AutomationApp.Interfaces;
using AutomationApp.Models;
using System;
using System.Threading.Tasks;

namespace AutomationApp.Rules
{
    /// <summary>
    /// Represents an automation rule for sending bulk emails to recipients loaded from a CSV file.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IAutomationRule"/> to integrate with the automation engine.
    /// Reads recipient data from a CSV file and uses an email service to send emails based on the provided configuration.
    /// </remarks>
    public class BulkEmailRule : IAutomationRule
    {
        private readonly IEmailService _emailService; // Service for sending emails
        private readonly IDataService _dataService;   // Service for reading CSV data
        private readonly EmailConfiguration _config; // Email settings (SMTP, IMAP, credentials)
        private readonly string _csvPath;            // Path to the CSV file containing recipients
        private string _name;                       // Backing field for RuleName

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkEmailRule"/> class.
        /// </summary>
        /// <param name="emailService">The service for sending emails. Cannot be null.</param>
        /// <param name="dataService">The service for reading CSV data. Cannot be null.</param>
        /// <param name="config">The email configuration settings. Cannot be null.</param>
        /// <param name="csvPath">The path to the CSV file containing recipient data. Cannot be null.</param>
        /// <param name="name">The name of the rule. Defaults to "BulkEmailRule" if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public BulkEmailRule(IEmailService emailService, IDataService dataService, EmailConfiguration config, string csvPath, string name = "BulkEmailRule")
        {
            // Validate dependencies
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _csvPath = csvPath ?? throw new ArgumentNullException(nameof(csvPath));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets or sets the unique name of the rule.
        /// </summary>
        /// <remarks>Defaults to "BulkEmailRule" if not specified in the constructor.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when setting to null.</exception>
        public string RuleName
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled for execution.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Executes the bulk email rule by reading recipients from a CSV file and sending emails.
        /// </summary>
        /// <param name="logger">The logger for recording execution details and errors. Cannot be null.</param>
        /// <returns>A task that resolves to true if the emails were sent successfully; otherwise, false.</returns>
        /// <remarks>
        /// Reads recipient data using <see cref="DataService"/> and sends emails via <see cref="EmailService.SendBulkEmailsAsync"/>.
        /// Logs execution status and errors using the provided logger.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public async Task<bool> ExecuteAsync(Logger logger)
        {
            // Validate logger
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                // Log start of execution
                logger.LogInfo($"Executing BulkEmailRule '{RuleName}' with CSV: {_csvPath}");

                // Validate CSV file existence
                if (!System.IO.File.Exists(_csvPath))
                {
                    logger.LogInfo($"CSV file not found: {_csvPath}");
                    return false;
                }

                // Read recipients from CSV
                var recipients = await _dataService.ReadCsvAsync<EmailRecipient>(_csvPath);
                if (recipients.Count == 0)
                {
                    logger.LogWarning($"No recipients loaded from {_csvPath}");
                    return false;
                }

                // Log number of recipients
                logger.LogInfo($"Loaded {recipients.Count} recipients from {_csvPath}");

                // Send bulk emails
                await _emailService.SendBulkEmailsAsync(recipients);
                logger.LogSuccess($"BulkEmailRule '{RuleName}' completed successfully. Sent {recipients.Count} emails.");

                return true;
            }
            catch (Exception ex)
            {
                // Log error and return failure
                logger.LogError(ex, $"Failed to execute BulkEmailRule '{RuleName}'");
                return false;
            }
        }
    }
}