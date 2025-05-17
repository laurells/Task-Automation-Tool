namespace AutomationApp.Models
{
    public class EmailConfig
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool UseSmtpSsl { get; set; }
        public string? ImapHost { get; set; }
        public int ImapPort { get; set; }
        public bool UseImapSsl { get; set; }
    }
}
