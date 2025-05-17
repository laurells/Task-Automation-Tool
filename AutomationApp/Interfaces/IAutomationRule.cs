namespace AutomationApp.Interfaces
{
    public interface IAutomationRule
    {
        string RuleName { get; }
        Task <bool> ExecuteAsync();
        bool Enabled { get; }
    }
}
