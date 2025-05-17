using AutomationApp.Interfaces;

public class AutomationEngine
{
    private readonly List<IAutomationRule> _rules = [];

    public void RegisterRule(IAutomationRule rule)
    {
        _rules.Add(rule);
        Console.WriteLine($"Rule registered: {rule.RuleName}");
    }

    public async Task ExecuteAllAsync()
    {
        foreach (var rule in _rules)
        {
            Console.WriteLine($"\nExecuting Rule: {rule.RuleName}");
            await rule.ExecuteAsync();
        }
    }
}
