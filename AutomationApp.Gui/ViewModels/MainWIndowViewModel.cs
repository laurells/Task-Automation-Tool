using ReactiveUI;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive;
using AutomationApp;
using AutomationApp.Interfaces;
using AutomationApp.Models;
using AutomationApp.Services;
using AutomationApp.Rules;
using System.IO;
using System.Text.Json;

namespace AutomationApp.Gui.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public ObservableCollection<IAutomationRule> Rules { get; } = new();
        private readonly AutomationEngine _engine;

        public ReactiveCommand<string, Unit> RunCommand { get; }
        public ReactiveCommand<Unit, Unit> RunAllCommand { get; }

        public MainWindowViewModel()
        {
            // Load configuration (adjust path if needed)
            var config = LoadConfiguration();
            var logger = new Logger("GUI");
            var fileService = new FileService(logger);
            var dataService = new DataService();
            var emailService = new EmailService(config.Email, logger);

            _engine = new AutomationEngine(logger);

            // Register rules from configuration (same as CLI)
            foreach (var rule in config.Rules)
            {
                if (!rule.Enabled) continue;

                switch (rule.Type.ToLower())
                {
                    case "filemoverule":
                        _engine.RegisterRule(new FileMoveRule(
                            rule.Settings["source"],
                            rule.Settings["target"],
                            fileService,
                            logger));
                        break;
                    case "bulkemailrule":
                        _engine.RegisterRule(new BulkEmailRule(
                            emailService,
                            dataService,
                            new EmailConfig()
                        ));
                        break;
                    default:
                        logger.LogWarning($"Unknown rule type: {rule.Type}");
                        break;
                }
            }

            // Add rules to observable collection
            foreach (var rule in _engine.Rules)
                Rules.Add(rule);

            RunCommand = ReactiveCommand.CreateFromTask<string>(async ruleName =>
            {
                var rule = Rules.FirstOrDefault(r => r.RuleName == ruleName);
                if (rule != null) await rule.ExecuteAsync();
            });

            RunAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _engine.ExecuteAllAsync();
            });
        }

        private AppConfiguration LoadConfiguration()
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<AppConfiguration>(json);
        }
    }
}