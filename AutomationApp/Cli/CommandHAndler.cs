using AutomationApp.Core;
using AutomationApp.Services;
using System.Text.Json;

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
                    case "configure":
                        await ConfigureCommand();
                        break;
                    case "list-rules":
                        ListRules();
                        break;
                    case "validate-rules":
                        ValidateRules();
                        break;
                    // case "delete-rule":
                    //     await DeleteRuleCommand(args);
                    //     break;
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
            Console.WriteLine("Task Automation Tool CLI Commands:");
            Console.WriteLine();
            Console.WriteLine("run - Execute all enabled rules");
            Console.WriteLine("schedule <interval> <unit> - Set scheduling interval (e.g., 'schedule 1 hour', 'schedule 30 minute')");
            Console.WriteLine("test - Test all rules without executing");
            Console.WriteLine("status - Show current status of rules");
            Console.WriteLine("configure - Open configuration interface");
            Console.WriteLine("list-rules - List all available rules");
            Console.WriteLine("validate-rules - Validate rule configurations");
            Console.WriteLine("delete-rule <rule-name> - Delete a rule by name");
            Console.WriteLine("help - Show this help message");
            _logger.LogInfo("");
            _logger.LogInfo("Examples:");
            _logger.LogInfo("  automation.exe run");
            _logger.LogInfo("  automation.exe schedule --interval 60");
            _logger.LogInfo("  automation.exe test filemoverule");
            _logger.LogInfo("  automation.exe configure");
            _logger.LogInfo("  automation.exe list-rules");
            _logger.LogInfo("  automation.exe validate-rules");
        }

        private async Task RunCommand()
        {
            _logger.LogInfo("Starting rule execution...");
            var result = await _engine.ExecuteAllAsync();
            _logger.LogInfo($"Rule execution completed. Success: {result}");
        }

        private async Task ScheduleCommand(string[] args)
        {
            int interval = 30;
            for (int i = 1; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]) || !args[i].StartsWith("--interval"))
                    continue;
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedInterval))
                    interval = parsedInterval;
                else
                {
                    _logger.LogWarning("Invalid interval value");
                    return;
                }
            }

            var scheduler = new AutomationScheduler(_engine, interval, _logger);
            _logger.LogInfo($"Scheduler will run every {interval} seconds");
            scheduler.Start();

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
                    var success = await rule.ExecuteAsync(logger: _logger);
                    if (success)
                    {
                        _logger.LogInfo($"Rule test completed successfully: {rule.RuleName}");
                    }
                    else
                    {
                        _logger.LogError(null, $"Rule test failed: {rule.RuleName}");
                    }
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
            }
            return Task.CompletedTask;
        }

        private async Task ConfigureCommand()
        {
            _logger.LogInfo("Starting rule configuration...");
            while (true)
            {
                _logger.LogInfo("\nSelect rule type to configure (or 'exit' to quit):");
                _logger.LogInfo("1. FileMoveRule");
                _logger.LogInfo("2. BulkEmailRule");
                _logger.LogInfo("3. DataProcessingRule");
                _logger.LogInfo("Enter '1', '2', or '3' to select a rule type, or 'exit' to quit:");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "exit")
                    break;

                if (input != "1" && input != "2" && input != "3")
                {
                    _logger.LogWarning("Invalid selection. Enter '1', '2', '3', or 'exit'.");
                    continue;
                }

                if (input == "1")
                    await ConfigureFileMoveRule();
                else if (input == "2")
                    await ConfigureBulkEmailRule();
                else if (input == "3")
                    await ConfigureDataProcessingRule();
            }
        }

        private void ListRules()
        {
            if (_engine.Rules.Count == 0)
            {
                _logger.LogInfo("No rules loaded.");
                Console.WriteLine("No rules loaded.");
                return;
            }

            _logger.LogInfo($"Loaded rules ({_engine.Rules.Count}):");
            Console.WriteLine($"Loaded rules ({_engine.Rules.Count}):");
            foreach (var rule in _engine.Rules)
            {
                _logger.LogInfo($"- {rule.RuleName} ({rule.GetType().Name})");
                Console.WriteLine($"- {rule.RuleName} ({rule.GetType().Name})");
            }
        }

        private void ValidateRules()
        {
            if (_engine.Rules.Count == 0)
            {
                _logger.LogInfo("No valid rules found.");
                Console.WriteLine("No valid rules found.");
                return;
            }

            _logger.LogInfo($"Valid rules: {_engine.Rules.Count}");
            Console.WriteLine($"Valid rules: {_engine.Rules.Count}");
        }

        private async Task ConfigureFileMoveRule()
        {
            _logger.LogInfo("Configuring FileMoveRule...");
            _logger.LogInfo("Enter rule name (e.g., MoveFilesTo_Target):");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Rule name cannot be empty.");
                return;
            }

            _logger.LogInfo("Enter source directory (e.g., C:\\Users\\USER\\Desktop\\Source):");
            var source = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(source))
            {
                _logger.LogWarning("Source directory cannot be empty.");
                return;
            }

            _logger.LogInfo("Enter target directory (e.g., C:\\Users\\USER\\Desktop\\Target):");
            var target = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(target))
            {
                _logger.LogWarning("Target directory cannot be empty.");
                return;
            }

            _logger.LogInfo("Enter supported extensions (e.g., .pdf,.txt or press Enter for all):");
            var extensionsInput = Console.ReadLine()?.Trim();
            var extensions = string.IsNullOrEmpty(extensionsInput) ? Array.Empty<string>() : extensionsInput.Split(',').Select(e => e.Trim()).ToArray();

            _logger.LogInfo("Add timestamp to duplicate files? (y/n):");
            var addTimestamp = Console.ReadLine()?.Trim().ToLower() == "y";

            _logger.LogInfo("Create backups? (y/n):");
            var backupFiles = Console.ReadLine()?.Trim().ToLower() == "y";

            var rule = new
            {
                type = "FileMoveRule",
                name,
                source,
                target,
                supportedExtensions = extensions,
                addTimestamp,
                backupFiles
            };

            await SaveRuleToConfig(rule);
            _logger.LogInfo($"FileMoveRule '{name}' configured successfully.");
        }

        private async Task ConfigureBulkEmailRule()
        {
            _logger.LogInfo("Configuring BulkEmailRule...");
            _logger.LogInfo("Enter rule name (e.g., SendEmailsFromCSV):");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Rule name cannot be empty.");
                return;
            }

            _logger.LogInfo("Enter CSV file path (e.g., recipients.csv):");
            var csvPath = Path.GetFullPath(Console.ReadLine()?.Trim() ?? "");
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                _logger.LogWarning("CSV file path is invalid or file does not exist.");
                return;
            }

            var rule = new
            {
                type = "BulkEmailRule",
                name,
                csvPath
            };

            await SaveRuleToConfig(rule);
            _logger.LogInfo($"BulkEmailRule '{name}' configured successfully.");
        }

        private async Task ConfigureDataProcessingRule()
        {
            _logger.LogInfo("Configuring DataProcessingRule...");
            _logger.LogInfo("Enter rule name (e.g., ProcessDataFromCSV):");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Rule name cannot be empty.");
                return;
            }

            _logger.LogInfo("Enter file path (e.g., data.csv or data.xlsx):");
            var filePath = Path.GetFullPath(Console.ReadLine()?.Trim() ?? "");
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File path is invalid or file does not exist.");
                return;
            }

            _logger.LogInfo("Enter required columns (comma-separated, e.g., id,name):");
            var columnsInput = Console.ReadLine()?.Trim();
            var requiredColumns = string.IsNullOrEmpty(columnsInput)
                ? Array.Empty<string>()
                : columnsInput.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToArray();

            if (requiredColumns.Length <= 0)
            {
                _logger.LogWarning("At least one required column must be specified.");
                return;
            }

            var rule = new
            {
                type = "DataProcessingRule",
                name,
                filePath,
                requiredColumns
            };

            await SaveRuleToConfig(rule);
            _logger.LogInfo($"DataProcessingRule '{name}' configured successfully.");
        }

        private async Task SaveRuleToConfig(object rule)
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.rules.json");
                List<object> rules = [];

                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    rules = JsonSerializer.Deserialize<List<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                }

                rules.Add(rule);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(rules, options);
                await File.WriteAllTextAsync(configPath, updatedJson);
                _logger.LogInfo($"Rule saved to {configPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save rule to config.rules.json");
            }
        }
    }
}