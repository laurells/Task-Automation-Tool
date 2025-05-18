using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomationApp.Services;
using AutomationApp.Core;

namespace AutomationApp.Cli
{
    public class CommandHandler
    {
        private readonly AutomationEngine _engine;
        private readonly Logger _logger;

        public CommandHandler(AutomationEngine engine, Logger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    ShowHelp();
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "run":
                        await RunCommand();
                        break;
                    case "schedule":
                        await ScheduleCommand(args);
                        break;
                    case "test":
                        await TestCommand();
                        break;
                    case "status":
                        await StatusCommand();
                        break;
                    case "help":
                    default:
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command");
                ShowHelp();
            }
        }

        private void ShowHelp()
        {
            _logger.LogInfo("Task Automation Tool - Command Line Interface");
            _logger.LogInfo("Available commands:");
            _logger.LogInfo("  run          - Execute all automation rules once");
            _logger.LogInfo("  schedule     - Start scheduled execution of rules");
            _logger.LogInfo("  test         - Test specific rules");
            _logger.LogInfo("  status       - Show current status and statistics");
            _logger.LogInfo("  help         - Show this help message");
            _logger.LogInfo("");
            _logger.LogInfo("Examples:");
            _logger.LogInfo("  automation.exe run");
            _logger.LogInfo("  automation.exe schedule --interval 60");
            _logger.LogInfo("  automation.exe test filemoverule");
        }

        private async Task RunCommand()
        {
            _logger.LogInfo("Starting rule execution...");
            var result = await _engine.ExecuteAllAsync();
            _logger.LogInfo($"Rule execution completed. Success: {result}");
        }

        private async Task ScheduleCommand(string[] args)
        {
            int interval = 30; // default 30 seconds

            for (int i = 1; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]) || !args[i].StartsWith("--interval"))
                {
                    continue;
                }
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedInterval))
                {
                    interval = parsedInterval;
                }
                else
                {
                    _logger.LogWarning("Invalid interval value");
                    return;
                }
            }

            var scheduler = new AutomationScheduler(_engine, interval, _logger);
            _logger.LogInfo($"Scheduler will run every {interval} seconds");
            _logger.LogInfo($"Starting scheduler with interval: {interval} seconds");
            scheduler.Start();

            // Wait for Ctrl+C or other termination signal
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("Stopping scheduler...");
                scheduler.Stop();
            }
        }

        private async Task TestCommand()
        {
            if (_engine.Rules.Count == 0)
            {
                _logger.LogWarning("No rules configured. Check appsettings.json and CSV files.");
                return;
            }

            _logger.LogInfo("Available rules:");
            foreach (var rule in _engine.Rules)
            {
                _logger.LogInfo($"- {rule.RuleName} ({rule.GetType().Name})");
            }

            while (true)
            {
                _logger.LogInfo("\nEnter rule name to test (or 'exit' to quit): ");
                var input = Console.ReadLine();

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (string.IsNullOrEmpty(input))
                {
                    _logger.LogWarning("Rule name cannot be empty");
                    continue;
                }

                var rule = _engine.Rules.FirstOrDefault(r => string.Equals(r.RuleName, input, StringComparison.OrdinalIgnoreCase));
                if (rule == null)
                {
                    _logger.LogWarning($"Rule not found: {input}");
                    continue;
                }

                _logger.LogInfo($"Testing rule: {rule.RuleName}");
                try
                {
                    await rule.ExecuteAsync();
                    _logger.LogInfo($"Rule test completed successfully: {rule.RuleName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Rule test failed: {rule.RuleName}");
                }
            }
        }

        private Task StatusCommand()
        {
            _logger.LogInfo("Task Automation Tool Status");
            _logger.LogInfo($"Rules configured: {_engine.Rules.Count}");

            foreach (var rule in _engine.Rules)
            {
                _logger.LogInfo($"- {rule.RuleName}");
                _logger.LogInfo($"  Type: {rule.GetType().Name}");
                _logger.LogInfo($"  Status: {(rule.Enabled ? "Enabled" : "Disabled")}");
                // _logger.LogInfo($"  Last Execution: {rule.LastExecutionTime ?? DateTime.MinValue}");
                // logger.LogInformation($"  Success: {rule.SuccessCount}");
                // logger.LogInformation($"  Failures: {rule.FailureCount}");
            }
            return Task.CompletedTask;
        }
    }
}

