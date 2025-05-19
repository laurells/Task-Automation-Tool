namespace AutomationApp.Models
{
    /// <summary>
    /// Represents configuration settings for email operations, including SMTP and IMAP settings.
    /// </summary>
    /// <remarks>
    /// Used to configure email-related automation rules or services, such as sending or receiving emails.
    /// Properties are nullable to allow partial configuration, with validation handled by the consuming service.
    /// </remarks>
    public class EmailConfig
    {
        /// <summary>
        /// Gets or sets the email address for authentication.
        /// </summary>
        /// <remarks>Nullable to allow configuration without an email until required.</remarks>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the password for email authentication.
        /// </summary>
        /// <remarks>Nullable to allow configuration without a password until required. Should be stored securely.</remarks>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the SMTP host for sending emails.
        /// </summary>
        /// <remarks>Nullable to allow configuration without an SMTP host until required. Example: "smtp.gmail.com".</remarks>
        public string? SmtpHost { get; set; }

        /// <summary>
        /// Gets or sets the SMTP port for sending emails.
        /// </summary>
        /// <remarks>Defaults to 0. Common values are 587 (TLS) or 465 (SSL).</remarks>
        public int SmtpPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is used for SMTP connections.
        /// </summary>
        /// <remarks>Defaults to false. Set to true for secure connections (e.g., TLS/SSL).</remarks>
        public bool UseSmtpSsl { get; set; }

        /// <summary>
        /// Gets or sets the IMAP host for receiving emails.
        /// </summary>
        /// <remarks>Nullable to allow configuration without an IMAP host until required. Example: "imap.gmail.com".</remarks>
        public string? ImapHost { get; set; }

        /// <summary>
        /// Gets or sets the IMAP port for receiving emails.
        /// </summary>
        /// <remarks>Defaults to 0. Common value is 993 (SSL).</remarks>
        public int ImapPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is used for IMAP connections.
        /// </summary>
        /// <remarks>Defaults to false. Set to true for secure connections (e.g., SSL).</remarks>
        public bool UseImapSsl { get; set; }
    }
}