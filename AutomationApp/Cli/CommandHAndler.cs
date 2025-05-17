using Microsoft.Extensions.Logging;

namespace AutomationApp.Cli
{
    public class CommandHandler
    {
        public static async Task HandleAsync(string[] args, AutomationEngine engine, ILogger logger)
        {
            try
            {
                if (args.Length == 0)
                {
                    ShowHelp(logger);
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "run":
                        await RunCommand(engine, logger);
                        break;
                    case "schedule":
                        await ScheduleCommand(engine, args, logger);
                        break;
                    case "test":
                        await TestCommand(engine, logger);
                        break;
                    case "status":
                        await StatusCommand(engine, logger);
                        break;
                    case "help":
                    default:
                        ShowHelp(logger);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing command");
                ShowHelp(logger);
            }
        }

        private static void ShowHelp(ILogger logger)
        {
            logger.LogInformation("Task Automation Tool - Command Line Interface");
            logger.LogInformation("Available commands:");
            logger.LogInformation("  run          - Execute all automation rules once");
            logger.LogInformation("  schedule     - Start scheduled execution of rules");
            logger.LogInformation("  test         - Test specific rules");
            logger.LogInformation("  status       - Show current status and statistics");
            logger.LogInformation("  help         - Show this help message");
            logger.LogInformation("");
            logger.LogInformation("Examples:");
            logger.LogInformation("  automation.exe run");
            logger.LogInformation("  automation.exe schedule --interval 60");
            logger.LogInformation("  automation.exe test filemoverule");
        }

        private static async Task RunCommand(AutomationEngine engine, ILogger logger)
        {
            logger.LogInformation("Starting rule execution...");
            var result = await engine.ExecuteAllAsync();
            logger.LogInformation($"Rule execution completed. Success: {result}");
        }

        private static async Task ScheduleCommand(AutomationEngine engine, string[] args, ILogger logger)
        {
            int interval = 30; // default 30 seconds
            
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("--interval"))
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedInterval))
                    {
                        interval = parsedInterval;
                    }
                    else
                    {
                        logger.LogError("Invalid interval value");
                        return;
                    }
                }
            }

            var scheduler = new AutomationScheduler(engine, interval);
            logger.LogInformation($"Starting scheduler with interval: {interval} seconds");
            scheduler.Start();

            // Wait for Ctrl+C or other termination signal
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Stopping scheduler...");
                scheduler.Stop();
            }
        }

        private static async Task TestCommand(AutomationEngine engine, ILogger logger)
        {
            if (engine.Rules.Count == 0)
            {
                logger.LogError("No rules configured");
                return;
            }

            logger.LogInformation("Available rules:");
            foreach (var rule in engine.Rules)
            {
                logger.LogInformation($"- {rule.RuleName} ({rule.GetType().Name})");
            }

            while (true)
            {
                logger.LogInformation("\nEnter rule name to test (or 'exit' to quit): ");
                var input = Console.ReadLine();

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                var rule = engine.Rules.FirstOrDefault(r => string.Equals(r.RuleName, input, StringComparison.OrdinalIgnoreCase));
                if (rule == null)
                {
                    logger.LogError($"Rule not found: {input}");
                    continue;
                }

                logger.LogInformation($"Testing rule: {rule.RuleName}");
                try
                {
                    await rule.ExecuteAsync();
                    logger.LogInformation($"Rule test completed successfully: {rule.RuleName}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Rule test failed: {rule.RuleName}");
                }
            }
        }

        private static Task StatusCommand(AutomationEngine engine, ILogger logger)
        {
            logger.LogInformation("Task Automation Tool Status");
            logger.LogInformation($"Rules configured: {engine.Rules.Count}");

            foreach (var rule in engine.Rules)
            {
                logger.LogInformation($"- {rule.RuleName}");
                logger.LogInformation($"  Type: {rule.GetType().Name}");
                // logger.LogInformation($"  Status: {(rule.Enabled ? "Enabled" : "Disabled")}");
                // logger.LogInformation($"  Last Execution: {rule.LastExecutionTime ?? DateTime.MinValue}");
                // logger.LogInformation($"  Success: {rule.SuccessCount}");
                // logger.LogInformation($"  Failures: {rule.FailureCount}");
            }
            return Task.CompletedTask;
        }
    }
}
