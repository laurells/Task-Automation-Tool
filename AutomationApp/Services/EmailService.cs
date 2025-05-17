using MailKit.Net.Smtp;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using AutomationApp.Models;
namespace AutomationApp.Services
{
    // Define EmailRecipient if it doesn't exist elsewhere
    public class EmailRecipient
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
    }

    public class EmailService
    {
        public static async Task SendEmailAsync(string smtpHost, int port, bool useSSL, string fromEmail, string password, string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();

            await client.ConnectAsync(smtpHost, port, useSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromEmail, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine("Email sent successfully.");
        }

        public async Task SendBulkEmailsAsync(EmailConfig config, List<EmailRecipient> recipients)
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSmtpSsl);
            await smtp.AuthenticateAsync(config.Email, config.Password);

            foreach (var r in recipients)
            {
                var msg = new MimeMessage();
                msg.From.Add(MailboxAddress.Parse(config.Email));
                msg.To.Add(MailboxAddress.Parse(r.Email));
                msg.Subject = $"Message for {r.Name}";
                msg.Body = new TextPart("plain") { Text = r.Message };

                await smtp.SendAsync(msg);
                Console.WriteLine($"Sent email to {r.Email}");
            }

            await smtp.DisconnectAsync(true);
        }


        public static async Task ReadInboxAsync(string imapHost, int port, bool useSSL, string email, string password)
        {
            using var client = new ImapClient();

            await client.ConnectAsync(imapHost, port, useSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(email, password);
            await client.Inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            Console.WriteLine("Reading latest 5 emails:\n");

            int count = client.Inbox.Count;
            for (int i = count - 1; i >= Math.Max(0, count - 5); i--)
            {
                var message = await client.Inbox.GetMessageAsync(i);
                Console.WriteLine($"From: {message.From}, Subject: {message.Subject}");
            }

            await client.DisconnectAsync(true);
        }

        public static async Task ApplyInboxRulesAsync(EmailConfig config)
        {
            using var client = new ImapClient();

            await client.ConnectAsync(config.ImapHost, config.ImapPort, config.UseImapSsl);
            await client.AuthenticateAsync(config.Email, config.Password);
            await client.Inbox.OpenAsync(MailKit.FolderAccess.ReadWrite);

            var uids = await client.Inbox.SearchAsync(MailKit.Search.SearchQuery.All);
            foreach (var uid in uids)
            {
                var message = await client.Inbox.GetMessageAsync(uid);

                // Rule: Delete emails from specific sender
                // if (message.From.ToString().Contains("spam@example.com"))
                // {
                //     await client.Inbox.AddFlagsAsync(uid, MailKit.MessageFlags.Deleted, true);
                //     Console.WriteLine($"Deleted spam from: {message.From}");
                // }

                // Rule: Auto-forward emails with subject containing "Urgent"
                if (message.Subject.Contains("Urgent", StringComparison.OrdinalIgnoreCase))
                {
                    await ForwardEmailAsync(config, message);
                }
            }

            await client.DisconnectAsync(true);
        }

        private static async Task ForwardEmailAsync(EmailConfig config, MimeMessage original)
        {
            var forward = new MimeMessage();
            forward.From.Add(MailboxAddress.Parse(config.Email));
            forward.To.Add(MailboxAddress.Parse("your-forwarding-email@example.com"));
            forward.Subject = $"FWD: {original.Subject}";
            forward.Body = new TextPart("plain") { Text = original.TextBody ?? "[No body content]" };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSmtpSsl);
            await smtp.AuthenticateAsync(config.Email, config.Password);
            await smtp.SendAsync(forward);
            await smtp.DisconnectAsync(true);

            Console.WriteLine("Forwarded urgent email.");
        }

    }
}
