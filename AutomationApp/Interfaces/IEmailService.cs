using AutomationApp.Models;

namespace AutomationApp.Interfaces
{
    /// <summary>
    /// Defines the contract for email sending and reading operations.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email to a single recipient with an optional template.
        /// </summary>
        /// <param name="recipient">The recipient details.</param>
        /// <param name="template">The optional email template.</param>
        /// <returns>A task that resolves to true if the email was sent successfully; otherwise, false.</returns>
        Task<bool> SendEmailAsync(EmailRecipient recipient, EmailTemplate? template = null);

        /// <summary>
        /// Reads emails from the inbox.
        /// </summary>
        /// <returns>A task that resolves to a list of email recipients extracted from inbox messages.</returns>
        Task<List<EmailRecipient>> ReadEmailsAsync();

        /// <summary>
        /// Sends emails to recipients listed in a CSV file.
        /// </summary>
        /// <param name="csvPath">The path to the CSV file.</param>
        /// <returns>A task that resolves to true if all emails were processed successfully; otherwise, false.</returns>
        Task<bool> SendEmailsFromCsvAsync(string csvPath);

        /// <summary>
        /// Sends bulk emails to multiple recipients.
        /// </summary>
        /// <param name="recipients">The list of recipients.</param>
        /// <returns>A task that resolves to true if all emails were sent successfully; otherwise, false.</returns>
        Task<bool> SendBulkEmailsAsync(List<EmailRecipient> recipients);
        Task SendEmailNullAsync(EmailRecipient recipient, EmailTemplate? template);
        Task<IEnumerable<EmailMessage>> ReadEmailsNullAsync();
        Task SendEmailsFromCsvNullAsync(string csvPath);
        Task SendBulkEmailsNullAsync(List<EmailRecipient> recipients);
    }
}