using AutomationApp.Services;
using AutomationApp.Utils;

var engine = new AutomationEngine();

// Load services
var fileService = new FileService();
var dataService = new DataService();
var emailService = new EmailService();
var emailConfig = Helpers.LoadEmailConfig();

// Register rules
engine.RegisterRule(new FileMoveRule(@"C:\Watch", @"C:\Sorted", fileService));
engine.RegisterRule(new BulkEmailRule(emailService, dataService, emailConfig));

// Handle CLI
await CommandHandler.HandleAsync(args, engine);


// var config = Helpers.LoadEmailConfig();
// var emailService = new EmailService();
// var fileService = new FileService();
// var fileWatcher = new FileWatcherService(fileService);

// await EmailService.ApplyInboxRulesAsync(config);

// string watchFolder = @"C:\Watch";
// string destinationFolder = @"C:\Sorted\PDFs";
// fileWatcher.StartWatching(watchFolder, "pdf", destinationFolder);
// Console.WriteLine("Press 'q' to quit the sample.");
// while (Console.Read() != 'q') ;
// fileWatcher.StopWatching();
// if (string.IsNullOrEmpty(config.SmtpHost))
// {
//     throw new ArgumentNullException(nameof(config.SmtpHost), "SMTP host cannot be null or empty.");
// }
// if (string.IsNullOrEmpty(config.Email))
// {
//     throw new ArgumentNullException(nameof(config.Email), "Email cannot be null or empty.");
// }
// if (string.IsNullOrEmpty(config.Password))
// {
//     throw new ArgumentNullException(nameof(config.Password), "Password cannot be null or empty.");
// }
// await EmailService.SendEmailAsync(
// 	config.SmtpHost,
// 	config.SmtpPort,
// 	config.UseSmtpSsl,
// 	config.Email,
// 	config.Password,
// 	$"File Watcher Status - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
// 	"File Watcher Status",
// 	"File watcher has stopped."
// );
// Console.WriteLine("File watcher has stopped.");
// if (string.IsNullOrEmpty(config.ImapHost))
// {
// 	throw new ArgumentNullException(nameof(config.ImapHost), "IMAP host cannot be null or empty.");
// }
// await EmailService.ReadInboxAsync(config.ImapHost, config.ImapPort, config.UseImapSsl, config.Email, config.Password);
// Console.WriteLine("Press any key to exit.");    
// Console.ReadKey();
// // End of file
