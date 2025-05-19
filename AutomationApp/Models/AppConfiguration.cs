using System.Collections.Generic;

namespace AutomationApp.Models
{
    /// <summary>
    /// Represents the overall configuration for the Task Automation Tool, including email, file, logging, and rule settings.
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Gets or sets the email configuration for SMTP and IMAP settings.
        /// </summary>
        /// <remarks>Initialized with a default <see cref="EmailConfiguration"/> instance.</remarks>
        public EmailConfiguration Email { get; set; } = new EmailConfiguration();

        /// <summary>
        /// Gets or sets the file configuration for default directories and supported extensions.
        /// </summary>
        /// <remarks>Initialized with a default <see cref="FileConfiguration"/> instance.</remarks>
        public FileConfiguration File { get; set; } = new FileConfiguration();

        /// <summary>
        /// Gets or sets the list of automation rules to be executed.
        /// </summary>
        /// <remarks>Initialized with an empty <see cref="List{T}"/> of <see cref="AutomationRule"/>.</remarks>
        public List<AutomationRule> Rules { get; set; } = new List<AutomationRule>();

        /// <summary>
        /// Gets or sets the logging configuration for log level and output settings.
        /// </summary>
        /// <remarks>Initialized with a default <see cref="LoggingConfiguration"/> instance.</remarks>
        public LoggingConfiguration Logging { get; set; } = new LoggingConfiguration();

        /// <summary>
        /// Gets the path to the rules configuration file.
        /// </summary>
        public string RulesConfigPath { get; init; } = "config.rules.json";

        public AppConfiguration(string rulesConfigPath, EmailConfiguration email, LoggingConfiguration logging)
        {
            if (string.IsNullOrEmpty(rulesConfigPath))
                throw new ArgumentException("Rules configuration path cannot be null or empty.", nameof(rulesConfigPath));
            RulesConfigPath = rulesConfigPath;
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            File = File ?? throw new ArgumentNullException(nameof(File));
            Rules = Rules ?? throw new ArgumentNullException(nameof(Rules));
        }
    }

    /// <summary>
    /// Represents configuration settings for email operations, including SMTP and IMAP.
    /// </summary>
    public class EmailConfiguration
    {
        /// <summary>
        /// Gets or sets the SMTP host for sending emails.
        /// </summary>
        /// <remarks>Defaults to "smtp.example.com".</remarks>
        public string SmtpHost { get; set; } = "smtp.example.com";

        /// <summary>
        /// Gets or sets the SMTP port for sending emails.
        /// </summary>
        /// <remarks>Defaults to 587 (TLS).</remarks>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Gets or sets a value indicating whether SSL is used for SMTP connections.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool UseSmtpSsl { get; set; } = true;

        /// <summary>
        /// Gets or sets the email address for authentication.
        /// </summary>
        /// <remarks>Defaults to an empty string.</remarks>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for email authentication.
        /// </summary>
        /// <remarks>Defaults to an empty string.</remarks>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IMAP host for receiving emails.
        /// </summary>
        /// <remarks>Defaults to "imap.example.com".</remarks>
        public string ImapHost { get; set; } = "imap.example.com";

        /// <summary>
        /// Gets or sets the IMAP port for receiving emails.
        /// </summary>
        /// <remarks>Defaults to 993 (SSL).</remarks>
        public int ImapPort { get; set; } = 993;

        /// <summary>
        /// Gets or sets a value indicating whether SSL is used for IMAP connections.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool UseImapSsl { get; set; } = true;
    }

    /// <summary>
    /// Represents configuration settings for file operations, including directories and extensions.
    /// </summary>
    public class FileConfiguration
    {
        /// <summary>
        /// Gets or sets the default source directory for file operations.
        /// </summary>
        /// <remarks>Defaults to "C:\Watch".</remarks>
        public string DefaultSourceDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Watch");

        /// <summary>
        /// Gets or sets the default target directory for file operations.
        /// </summary>
        /// <remarks>Defaults to "C:\Sorted".</remarks>
        public string DefaultTargetDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Sorted");

        /// <summary>
        /// Gets or sets the list of supported file extensions for operations.
        /// </summary>
        /// <remarks>Defaults to common extensions like .pdf, .doc, .xlsx, .csv, etc.</remarks>
        public List<string> SupportedExtensions { get; set; } = new List<string>
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".json", ".xml"
        };

        /// <summary>
        /// Gets or sets a value indicating whether backups are created during file operations.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory for storing backup files.
        /// </summary>
        /// <remarks>Defaults to "C:\Backup".</remarks>
        public string BackupDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Backup");
    }

    /// <summary>
    /// Represents configuration settings for logging, including log level and output options.
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum log level for recording messages.
        /// </summary>
        /// <remarks>Defaults to <see cref="LogLevel.Info"/>.</remarks>
        public LogLevel LogLevel { get; set; } = new LogLevel();

        /// <summary>
        /// Gets or sets the directory for storing log files.
        /// </summary>
        /// <remarks>Defaults to "C:\Logs".</remarks>
        public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "logs");

        /// <summary>
        /// Gets or sets a value indicating whether log messages are output to the console.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool EnableConsoleOutput { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether log messages are written to files.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether error messages are logged.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool EnableErrorLogging { get; set; } = true;
    }

    /// <summary>
    /// Represents an automation rule configuration, including its type, settings, and schedule.
    /// </summary>
    public class AutomationRule
    {
        /// <summary>
        /// Gets or sets the type of the automation rule (e.g., "FileMoveRule", "DataProcessingRule").
        /// </summary>
        /// <remarks>Defaults to an empty string.</remarks>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique name of the automation rule.
        /// </summary>
        /// <remarks>Defaults to an empty string.</remarks>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dictionary of rule-specific settings.
        /// </summary>
        /// <remarks>Uses <see cref="object"/> to support flexible key-value pairs. Initialized as an empty <see cref="Dictionary{TKey, TValue}"/>.</remarks>
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled for execution.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the cron schedule for the rule's execution.
        /// </summary>
        /// <remarks>Defaults to "* * * * *" (every minute).</remarks>
        public string Schedule { get; set; } = "* * * * *";
    }

    /// <summary>
    /// Defines the available logging levels for the application.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed diagnostic information for debugging.
        /// </summary>
        Debug,

        /// <summary>
        /// General information about application operations.
        /// </summary>
        Info,

        /// <summary>
        /// Potential issues that may require attention.
        /// </summary>
        Warning,

        /// <summary>
        /// Successful operations or milestones.
        /// </summary>
        Success,

        /// <summary>
        /// Errors that prevent normal operation.
        /// </summary>
        Error,

        /// <summary>
        /// Fine-grained diagnostic information for tracing execution.
        /// </summary>
        Trace
    }
}