namespace AutomationApp.Interfaces
{
    public interface IAutomationRule
    {
        string RuleName { get; }
        Task ExecuteAsync();
    }
}
