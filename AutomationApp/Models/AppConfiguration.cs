using System.Collections.Generic;

namespace AutomationApp.Models
{
    public class AppConfiguration
    {
        public EmailConfiguration Email { get; set; } = new EmailConfiguration();
        public FileConfiguration File { get; set; } = new FileConfiguration();
        public List<AutomationRule> Rules { get; set; } = new List<AutomationRule>();
        public LoggingConfiguration Logging { get; set; } = new LoggingConfiguration();
    }

    public class EmailConfiguration
    {
        public string SmtpHost { get; set; } = "smtp.example.com";
        public int SmtpPort { get; set; } = 587;
        public bool UseSmtpSsl { get; set; } = true;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ImapHost { get; set; } = "imap.example.com";
        public int ImapPort { get; set; } = 993;
        public bool UseImapSsl { get; set; } = true;
    }

    public class FileConfiguration
    {
        public string DefaultSourceDirectory { get; set; } = "C:\\Watch";
        public string DefaultTargetDirectory { get; set; } = "C:\\Sorted";
        public List<string> SupportedExtensions { get; set; } = new List<string>
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".json", ".xml"
        };
        public bool CreateBackup { get; set; } = true;
        public string BackupDirectory { get; set; } = "C:\\Backup";
    }

    public class LoggingConfiguration
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public string LogDirectory { get; set; } = "C:\\Logs";
        public bool EnableConsoleOutput { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableErrorLogging { get; set; } = true;
    }

    public class AutomationRule
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>(); // Changed to object
        public bool Enabled { get; set; } = true;
        public string Schedule { get; set; } = "* * * * *";
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Success,
        Error,
        Trace,
    }
}