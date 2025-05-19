using AutomationApp.Services;
using AutomationApp.Core;
using System.Text.Json;

namespace AutomationApp.Cli
{
    /// <summary>
    /// Handles command-line interface (CLI) commands for the Task Automation Tool.
    /// Processes user inputs to execute, schedule, test, or configure automation rules.
    /// </summary>
    public class CommandHandler
    {
        private readonly AutomationEngine _engine;
        private readonly Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandler"/> class.
        /// </summary>
        /// <param name="engine">The automation engine to execute rules. Cannot be null.</param>
        /// <param name="logger">The logger for recording operations and errors. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> or <paramref name="logger"/> is null.</exception>
        public CommandHandler(AutomationEngine engine, Logger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes CLI arguments asynchronously and routes to appropriate command handlers.
        /// </summary>
        /// <param name="args">The command-line arguments provided by the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Logs and handles any uncaught exceptions, showing help.</exception>
        public async Task HandleAsync(string[] args)
        {
            try
            {
                // Check if no arguments are provided
                if (args.Length == 0)
                {
                    ShowHelp();
                    return;
                }

                // Route to command based on first argument
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

        /// <summary>
        /// Displays the CLI help message with available commands and usage examples.
        /// </summary>
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
            _logger.LogInfo("  automation.exe configure");
        }

        /// <summary>
        /// Executes all configured automation rules once.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RunCommand()
        {
            _logger.LogInfo("Starting rule execution...");
            var result = await _engine.ExecuteAllAsync();
            _logger.LogInfo($"Rule execution completed. Success: {result}");
        }

        /// <summary>
        /// Schedules rule execution at a specified interval.
        /// </summary>
        /// <param name="args">Command-line arguments, including optional --interval parameter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ScheduleCommand(string[] args)
        {
            // Default interval is 30 seconds
            int interval = 30;

            // Parse --interval argument if provided
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

            // Initialize and start the scheduler
            var scheduler = new AutomationScheduler(_engine, interval, _logger);
            _logger.LogInfo($"Scheduler will run every {interval} seconds");
            _logger.LogInfo($"Starting scheduler with interval: {interval} seconds");
            scheduler.Start();

            // Wait for termination signal (e.g., Ctrl+C)
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

        /// <summary>
        /// Tests specific rules interactively by allowing the user to select and execute them.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task TestCommand()
        {
            // Check if any rules are configured
            if (_engine.Rules.Count == 0)
            {
                _logger.LogWarning("No rules configured. Check appsettings.json and CSV files.");
                return;
            }

            // List available rules
            _logger.LogInfo("Available rules:");
            foreach (var rule in _engine.Rules)
            {
                _logger.LogInfo($"- {rule.RuleName} ({rule.GetType().Name})");
            }

            // Interactive rule testing loop
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

                // Find the rule by name (case-insensitive)
                var rule = _engine.Rules.FirstOrDefault(r => string.Equals(r.RuleName, input, StringComparison.OrdinalIgnoreCase));
                if (rule == null)
                {
                    _logger.LogWarning($"Rule not found: {input}");
                    continue;
                }

                // Execute the selected rule
                _logger.LogInfo($"Testing rule: {rule.RuleName}");
                try
                {
                    await rule.ExecuteAsync(logger: _logger);
                    _logger.LogInfo($"Rule test completed successfully: {rule.RuleName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Rule test failed: {rule.RuleName}");
                }
            }
        }

        /// <summary>
        /// Displays the status of configured rules, including their type and enabled state.
        /// </summary>
        /// <returns>A completed task.</returns>
        private Task StatusCommand()
        {
            _logger.LogInfo("Task Automation Tool Status");
            _logger.LogInfo($"Rules configured: {_engine.Rules.Count}");

            // Log details for each rule
            foreach (var rule in _engine.Rules)
            {
                _logger.LogInfo($"- {rule.RuleName}");
                _logger.LogInfo($"  Type: {rule.GetType().Name}");
                _logger.LogInfo($"  Status: {(rule.Enabled ? "Enabled" : "Disabled")}");
                // Note: Uncomment below for additional statistics when implemented
                // _logger.LogInfo($"  Last Execution: {rule.LastExecutionTime ?? DateTime.MinValue}");
                // _logger.LogInfo($"  Success: {rule.SuccessCount}");
                // _logger.LogInfo($"  Failures: {rule.FailureCount}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Interactively configures new automation rules and saves them to the configuration file.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ConfigureCommand()
        {
            _logger.LogInfo("Starting rule configuration...");
            while (true)
            {
                // Display rule type options
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

                // Route to specific rule configuration
                if (input == "1")
                {
                    await ConfigureFileMoveRule();
                }
                else if (input == "2")
                {
                    await ConfigureBulkEmailRule();
                }
                else if (input == "3")
                {
                    await ConfigureDataProcessingRule();
                }
            }
        }

        /// <summary>
        /// Configures a FileMoveRule by prompting for settings and saves it to the configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

            // Create rule configuration object
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

        /// <summary>
        /// Configures a BulkEmailRule by prompting for settings and saves it to the configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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
            var csvPath = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                _logger.LogWarning("CSV file path is invalid or file does not exist.");
                return;
            }

            // Create rule configuration object
            var rule = new
            {
                type = "BulkEmailRule",
                name,
                csvPath
            };

            await SaveRuleToConfig(rule);
            _logger.LogInfo($"BulkEmailRule '{name}' configured successfully.");
        }

        /// <summary>
        /// Configures a DataProcessingRule by prompting for settings and saves it to the configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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
            var filePath = Console.ReadLine()?.Trim();
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

            if (!requiredColumns.Any())
            {
                _logger.LogWarning("At least one required column must be specified.");
                return;
            }

            // Create rule configuration object
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

        /// <summary>
        /// Saves a rule configuration to the config.rules.json file.
        /// </summary>
        /// <param name="rule">The rule configuration object to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SaveRuleToConfig(object rule)
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.rules.json");
                List<object> rules = [];

                // Load existing rules if config file exists
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    rules = JsonSerializer.Deserialize<List<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                }

                // Add new rule and save to file
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