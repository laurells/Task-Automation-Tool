using AutomationApp.Interfaces;
using AutomationApp.Services;

namespace AutomationApp.Core
{
    /// <summary>
    /// Manages the execution and registration of automation rules, tracking their statistics.
    /// </summary>
    public class AutomationEngine
    {
        private readonly List<IAutomationRule> _rules = [];
        private readonly Logger _logger;
        private readonly Dictionary<string, RuleStatistics> _ruleStats = [];
        private readonly object _statsLock = new();

        /// <summary>
        /// Gets the list of registered automation rules.
        /// </summary>
        public List<IAutomationRule> Rules => _rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationEngine"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording operations and errors. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public AutomationEngine(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers an automation rule and initializes its statistics.
        /// </summary>
        /// <param name="rule">The automation rule to register. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
        public void RegisterRule(IAutomationRule rule)
        {
            // Validate input
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            // Add rule to collection and initialize stats
            _rules.Add(rule);
            _ruleStats[rule.RuleName] = new RuleStatistics();
            _logger.LogInfo($"Rule registered: {rule.RuleName}");
        }

        /// <summary>
        /// Executes all enabled automation rules concurrently and returns the overall success status.
        /// </summary>
        /// <returns>A task that resolves to true if all enabled rules executed successfully; otherwise, false.</returns>
        public async Task<bool> ExecuteAllAsync()
        {
            // Track overall success across all rules
            var overallSuccess = true;
            var tasks = new List<Task<bool>>();

            // Queue execution tasks for enabled rules
            foreach (var rule in _rules)
            {
                if (!rule.Enabled)
                {
                    _logger.LogInfo($"Skipping disabled rule: {rule.RuleName}");
                    continue;
                }

                tasks.Add(ExecuteRuleAsync(rule));
            }

            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Determine overall success
            foreach (var result in results)
            {
                if (!result)
                {
                    overallSuccess = false;
                }
            }

            return overallSuccess;
        }

        /// <summary>
        /// Executes a single automation rule and updates its statistics.
        /// </summary>
        /// <param name="rule">The automation rule to execute. Cannot be null.</param>
        /// <returns>A task that resolves to true if the rule executed successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
        private async Task<bool> ExecuteRuleAsync(IAutomationRule rule)
        {
            // Validate input
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            try
            {
                _logger.LogInfo($"\nExecuting Rule: {rule.RuleName}");

                // Measure execution time
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var success = await rule.ExecuteAsync(logger: _logger);
                stopwatch.Stop();

                // Update statistics in a thread-safe manner
                lock (_statsLock)
                {
                    var stats = _ruleStats[rule.RuleName];
                    stats.LastExecutionTime = DateTime.Now;
                    stats.ExecutionTime = stopwatch.Elapsed;
                    stats.SuccessCount += success ? 1 : 0;
                    stats.FailureCount += success ? 0 : 1;
                    stats.LastSuccess = success;
                }

                // Log execution result
                if (success)
                {
                    _logger.LogSuccess($"Rule executed successfully: {rule.RuleName}");
                }
                else
                {
                    _logger.LogWarning($"Rule execution failed: {rule.RuleName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                // Log error and update failure stats
                _logger.LogError(ex, $"Error executing rule: {rule.RuleName}");
                lock (_statsLock)
                {
                    var stats = _ruleStats[rule.RuleName];
                    stats.FailureCount++;
                    stats.LastSuccess = false;
                    stats.LastErrorMessage = ex.Message;
                }
                return false;
            }
        }

        /// <summary>
        /// Retrieves the statistics for a specific rule.
        /// </summary>
        /// <param name="ruleName">The name of the rule to retrieve statistics for.</param>
        /// <returns>The <see cref="RuleStatistics"/> for the rule, or a new instance if not found.</returns>
        public RuleStatistics GetRuleStatistics(string ruleName)
        {
            // Return existing stats or a new instance if rule not found
            if (_ruleStats.TryGetValue(ruleName, out var stats))
            {
                return stats;
            }
            return new RuleStatistics();
        }
    }

    /// <summary>
    /// Represents execution statistics for an automation rule.
    /// </summary>
    public class RuleStatistics
    {
        /// <summary>
        /// Gets or sets the time of the last execution attempt.
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the duration of the last execution.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the last execution was successful.
        /// </summary>
        public bool LastSuccess { get; set; }

        /// <summary>
        /// Gets or sets the error message from the last failed execution, if any.
        /// </summary>
        public string LastErrorMessage { get; set; } = string.Empty;
    }
}