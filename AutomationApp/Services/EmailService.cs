using MailKit.Net.Smtp;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using AutomationApp.Models;
using AutomationApp.Services;
using System.Globalization;
using Microsoft.Extensions.Logging;
using CsvHelper;

namespace AutomationApp.Services
{
    public class EmailRecipient
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> Attachments { get; set; } = new List<string>();
        
    }

    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    public class EmailService
    {
        private readonly EmailConfiguration _config;
        private readonly Logger _logger;

        public EmailService(EmailConfiguration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailRecipient recipient, EmailTemplate? template = null)
        {
            try
            {
                if (string.IsNullOrEmpty(recipient.Email))
                {
                    _logger.LogWarning($"Invalid email address for recipient {recipient.Name}");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_config.Email));
                message.To.Add(MailboxAddress.Parse(recipient.Email));

                // Use template if provided, otherwise use recipient's subject/body
                message.Subject = template?.Subject ?? recipient.Subject;
                
                var bodyBuilder = new BodyBuilder();
                
                if (template != null)
                {
                    // Replace template variables
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

                // Add attachments if any
                foreach (var attachment in recipient.Attachments)
                {
                    try
                    {
                        if (File.Exists(attachment))
                        {
                            bodyBuilder.Attachments.Add(attachment);
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

                using var client = new SmtpClient();
                try
                {
                    await client.ConnectAsync(_config.SmtpHost, _config.SmtpPort, _config.UseSmtpSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_config.Email, _config.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    _logger.LogSuccess($"Email sent successfully to {recipient.Email}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email to {recipient.Email}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing email for recipient {recipient.Name}");
                return false;
            }
        }

        public async Task<List<EmailRecipient>> ReadEmailsAsync()
        {
            var recipients = new List<EmailRecipient>();
            try
            {
                using var client = new ImapClient();
                await client.ConnectAsync(_config.ImapHost, _config.ImapPort, _config.UseImapSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config.Email, _config.Password);

                var inbox = client.Inbox;
                await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

                for (int i = 0; i < inbox.Count; i++)
                {
                    var message = await inbox.GetMessageAsync(i);
                    var recipient = new EmailRecipient
                    {
                        Name = message.From.Mailboxes.FirstOrDefault()?.Name ?? string.Empty,
                        Email = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                        Subject = message.Subject ?? string.Empty,
                        Body = message.TextBody ?? string.Empty
                    };
                    recipients.Add(recipient);
                }

                await client.DisconnectAsync(true);
                _logger.LogInfo($"Read {recipients.Count} emails from inbox");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading emails from inbox");
            }

            return recipients;
        }

        public async Task<bool> SendEmailsFromCsvAsync(string csvPath)
        {
            try
            {
                if (!File.Exists(csvPath))
                {
                    _logger.LogError(new FileNotFoundException($"CSV file not found: {csvPath}"), $"CSV file not found: {csvPath}");
                    return false;
                }

                var recipients = new List<EmailRecipient>();
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                while (await csv.ReadAsync())
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

                foreach (var recipient in recipients)
                {
                    await SendEmailAsync(recipient);
                }

                _logger.LogSuccess($"Processed {recipients.Count} emails from CSV");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing CSV file: {csvPath}");
                return false;
            }
        }

        public async Task SendBulkEmailsAsync(EmailConfig config, List<EmailRecipient> recipients)
        {
            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSmtpSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(config.Email, config.Password);

                foreach (var recipient in recipients)
                {
                    var message = new MimeMessage();
                    message.From.Add(MailboxAddress.Parse(config.Email));
                    message.To.Add(MailboxAddress.Parse(recipient.Email));
                    message.Subject = recipient.Subject;
                    message.Body = new TextPart("plain") { Text = recipient.Body };

                    await smtp.SendAsync(message);
                    _logger.LogInfo($"Email sent to {recipient.Email}");
                }

                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk emails");
                throw;
            }
        }
    }
}
