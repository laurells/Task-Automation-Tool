using AutomationApp.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutomationApp.Services;

public class AutomationEngine
{
    private readonly List<IAutomationRule> _rules = new List<IAutomationRule>();
    private readonly Logger _logger;
    private readonly Dictionary<string, RuleStatistics> _ruleStats = new Dictionary<string, RuleStatistics>();
    private readonly object _statsLock = new object();

    public AutomationEngine(Logger logger)
    {
        _logger = logger;
    }

    public List<IAutomationRule> Rules => _rules;

    public void RegisterRule(IAutomationRule rule)
    {
        _rules.Add(rule);
        _ruleStats[rule.RuleName] = new RuleStatistics();
        _logger.LogInfo($"Rule registered: {rule.RuleName}");
    }

    public async Task<bool> ExecuteAllAsync()
    {
        var overallSuccess = true;
        var tasks = new List<Task<bool>>();

        foreach (var rule in _rules)
        {
            if (!rule.Enabled) continue;

            var task = ExecuteRuleAsync(rule);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            if (!result) overallSuccess = false;
        }

        return overallSuccess;
    }

    private async Task<bool> ExecuteRuleAsync(IAutomationRule rule)
    {
        try
        {
            _logger.LogInfo($"\nExecuting Rule: {rule.RuleName}");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var success = await rule.ExecuteAsync();
            stopwatch.Stop();

            lock (_statsLock)
            {
                var stats = _ruleStats[rule.RuleName];
                stats.LastExecutionTime = DateTime.Now;
                stats.ExecutionTime = stopwatch.Elapsed;
                stats.SuccessCount += success ? 1 : 0;
                stats.FailureCount += success ? 0 : 1;
                stats.LastSuccess = success;
            }

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
            _logger.LogError(ex, $"Error executing rule: {rule.RuleName}");
            lock (_statsLock)
            {
                var stats = _ruleStats[rule.RuleName];
                stats.FailureCount++;
                stats.LastSuccess = false;
            }
            return false;
        }
    }

    public RuleStatistics GetRuleStatistics(string ruleName)
    {
        if (_ruleStats.TryGetValue(ruleName, out var stats))
        {
            return stats;
        }
        return new RuleStatistics();
    }
}

public class RuleStatistics
{
    public DateTime? LastExecutionTime { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public bool LastSuccess { get; set; }
    public string LastErrorMessage { get; set; } = string.Empty;
}
