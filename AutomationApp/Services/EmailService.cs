using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutomationApp.Core;
using AutomationApp.Models;
using AutomationApp.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AutomationApp.Services
{
    /// <summary>
    /// A service for sending and reading emails using SMTP and IMAP protocols.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IEmailService"/> to provide email operations for the automation framework.
    /// Uses <see cref="MailKit.Net.Smtp"/> for sending emails, <see cref="MailKit.Net.Imap"/> for reading emails,
    /// and <see cref="CsvHelper"/> for processing recipient CSV files.
    /// </remarks>
    public class EmailService : IEmailService
    {
        private readonly EmailConfiguration _config; // Email server configuration
        private readonly Logger _logger;             // Logger for operation details

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailService"/> class.
        /// </summary>
        /// <param name="config">The email server configuration. Cannot be null.</param>
        /// <param name="logger">The logger for recording operation details. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="logger"/> is null.</exception>
        public EmailService(EmailConfiguration config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends an email to a single recipient with an optional template.
        /// </summary>
        /// <param name="recipient">The recipient details. Cannot be null.</param>
        /// <param name="template">The optional email template for personalization.</param>
        /// <returns>A task that resolves to true if the email was sent successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="recipient"/> is null.</exception>
        /// <remarks>
        /// Validates the recipient’s email address and sends the email using the configured SMTP server.
        /// Supports attachments and template variable replacement.
        /// </remarks>
        public async Task<bool> SendEmailAsync(EmailRecipient recipient, EmailTemplate? template = null)
        {
            // Validate input
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));

            try
            {
                // Validate email address
                var emailValidator = new EmailAddressAttribute();
                if (string.IsNullOrEmpty(recipient.Email) || !emailValidator.IsValid(recipient.Email))
                {
                    _logger.LogWarning($"Invalid email address for recipient: {recipient.Name} ({recipient.Email})");
                    return false;
                }

                // Create email message
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_config.Email));
                message.To.Add(MailboxAddress.Parse(recipient.Email));
                message.Subject = template?.Subject ?? recipient.Subject;

                var bodyBuilder = new BodyBuilder();

                // Apply template or use recipient body
                if (template != null)
                {
                    var processedBody = template.Body;
                    foreach (var variable in template.Variables)
                    {
                        processedBody = processedBody.Replace($"{{{{ {variable.Key} }}}}", variable.Value);
                    }
                    bodyBuilder.HtmlBody = processedBody;
                }
                else
                {
                    bodyBuilder.HtmlBody = recipient.Body;
                }

                // Add attachments
                foreach (var attachment in recipient.Attachments ?? new List<string>())
                {
                    try
                    {
                        if (File.Exists(attachment))
                        {
                            bodyBuilder.Attachments.Add(attachment);
                            _logger.LogDebug($"Added attachment: {attachment}");
                        }
                        else
                        {
                            _logger.LogWarning($"Attachment not found: {attachment}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to add attachment: {attachment}");
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                // Send email
                using var client = new SmtpClient();
                await client.ConnectAsync(_config.SmtpHost, _config.SmtpPort, _config.UseSmtpSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config.Email, _config.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogSuccess($"Email sent successfully to {recipient.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {recipient.Name} ({recipient.Email})");
                return false;
            }
        }

        /// <summary>
        /// Reads emails from the inbox.
        /// </summary>
        /// <returns>A task that resolves to a list of email recipients extracted from inbox messages.</returns>
        /// <remarks>
        /// Connects to the configured IMAP server and reads messages from the inbox.
        /// Extracts sender name, email, subject, and body into <see cref="EmailRecipient"/> objects.
        /// </remarks>
        public async Task<List<EmailRecipient>> ReadEmailsAsync()
        {
            var recipients = new List<EmailRecipient>();
            try
            {
                // Connect to IMAP server
                using var client = new ImapClient();
                await client.ConnectAsync(_config.ImapHost, _config.ImapPort, _config.UseImapSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config.Email, _config.Password);

                // Open inbox
                var inbox = client.Inbox;
                await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

                // Read messages
                for (int i = 0; i < inbox.Count; i++)
                {
                    var message = await inbox.GetMessageAsync(i);
                    var recipient = new EmailRecipient
                    {
                        Name = message.From.Mailboxes.FirstOrDefault()?.Name ?? string.Empty,
                        Email = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                        Subject = message.Subject ?? string.Empty,
                        Body = message.TextBody ?? message.HtmlBody ?? string.Empty
                    };
                    recipients.Add(recipient);
                    _logger.LogDebug($"Read email from {recipient.Email} with subject: {recipient.Subject}");
                }

                await client.DisconnectAsync(true);
                _logger.LogInfo($"Read {recipients.Count} emails from inbox");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read emails from inbox");
            }

            return recipients;
        }

        /// <summary>
        /// Sends emails to recipients listed in a CSV file.
        /// </summary>
        /// <param name="csvPath">The path to the CSV file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to true if all emails were processed successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="csvPath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the CSV file does not exist.</exception>
        /// <remarks>
        /// Reads recipients from a CSV file with columns: Name, Email, Subject, Body.
        /// Sends an email to each valid recipient using <see cref="SendEmailAsync"/>.
        /// </remarks>
        public async Task<bool> SendEmailsFromCsvAsync(string csvPath)
        {
            // Validate input
            if (string.IsNullOrEmpty(csvPath))
                throw new ArgumentException("CSV path cannot be null or empty.", nameof(csvPath));
            if (!File.Exists(csvPath))
            {
                _logger.LogInfo($"CSV file not found: {csvPath}");
                throw new FileNotFoundException($"CSV file not found: {csvPath}");
            }

            try
            {
                // Read CSV
                var recipients = new List<EmailRecipient>();
                using var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

                // Validate headers
                await csv.ReadAsync();
                csv.ReadHeader();
                var requiredHeaders = new[] { "Name", "Email", "Subject", "Body" };
                foreach (var header in requiredHeaders)
                {

                    if (!csv.HeaderRecord.Contains(header, StringComparer.OrdinalIgnoreCase))
                    {

                        _logger.LogInfo($"Missing required header '{header}' in CSV: {csvPath}");
                        
                        return false;
                    }
                }

                // Parse recipients
                while (await csv.ReadAsync())
                {
                    try
                    {
                        var recipient = new EmailRecipient
                        {
                            Name = csv.GetField("Name") ?? string.Empty,
                            Email = csv.GetField("Email") ?? string.Empty,
                            Subject = csv.GetField("Subject") ?? string.Empty,
                            Body = csv.GetField("Body") ?? string.Empty
                        };
                        recipients.Add(recipient);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Skipping invalid row {csv.Context.Parser.Row} in CSV: {csvPath}");
                    }
                }

                if (!recipients.Any())
                {
                    _logger.LogWarning($"No valid recipients found in CSV: {csvPath}");
                    return false;
                }

                // Send emails
                int sent = 0;
                foreach (var recipient in recipients)
                {
                    if (await SendEmailAsync(recipient))
                        sent++;
                }

                _logger.LogSuccess($"Processed {recipients.Count} recipients from CSV: {csvPath}. Sent: {sent}, Failed: {recipients.Count - sent}");
                return sent == recipients.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process CSV file: {csvPath}");
                return false;
            }
        }

        /// <summary>
        /// Sends bulk emails to multiple recipients.
        /// </summary>
        /// <param name="recipients">The list of recipients. Cannot be null.</param>
        /// <returns>A task that resolves to true if all emails were sent successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="recipients"/> is null.</exception>
        /// <remarks>
        /// Sends emails to each recipient using <see cref="SendEmailAsync"/> with a single SMTP connection.
        /// </remarks>
        public async Task<bool> SendBulkEmailsAsync(List<EmailRecipient> recipients)
        {
            // Validate input
            if (recipients == null)
                throw new ArgumentNullException(nameof(recipients));

            try
            {
                // Use single SMTP connection
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config.SmtpHost, _config.SmtpPort, _config.UseSmtpSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config.Email, _config.Password);

                int sent = 0;
                foreach (var recipient in recipients)
                {
                    if (await SendEmailAsync(recipient))
                        sent++;
                    await Task.Delay(100); // Throttle to avoid server overload
                }

                await smtp.DisconnectAsync(true);
                _logger.LogSuccess($"Sent {sent} of {recipients.Count} bulk emails");
                return sent == recipients.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk emails");
                return false;
            }
        }

        public Task SendEmailNullAsync(EmailRecipient recipient, EmailTemplate? template)
        {
            throw new NotImplementedException();
        }
        public Task<IEnumerable<EmailMessage>> ReadEmailsNullAsync()
        {
            throw new NotImplementedException();
        }
        public Task SendEmailsFromCsvNullAsync(string csvPath)
        {
            throw new NotImplementedException();
        }
        public Task SendBulkEmailsNullAsync(List<EmailRecipient> recipients)
        {
            throw new NotImplementedException();
        }
    }
    
}