using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutomationApp.Models
{
    /// <summary>
    /// Represents an email recipient with details for sending an email.
    /// </summary>
    public class EmailRecipient
    {
        /// <summary>
        /// Gets or sets the name of the recipient.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address of the recipient.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subject of the email.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body of the email.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of attachment file paths.
        /// </summary>
        public List<string> Attachments { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents an email template with a subject, body, and variables for personalization.
    /// </summary>
    public class EmailTemplate
    {
        /// <summary>
        /// Gets or sets the subject of the email template.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body of the email template.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the variables for replacing placeholders in the template.
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    public class EmailMessage
    {
        /// <summary>
        /// Gets or sets the sender’s email address.
        /// </summary>
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email subject.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email body.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date the email was received.
        /// </summary>
        public DateTime ReceivedDate { get; set; }
    }
}