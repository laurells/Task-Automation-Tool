using AutomationApp.Core;
using AutomationApp.Services;   

namespace AutomationApp.Interfaces
{
    /// <summary>
    /// Defines the contract for automation rules that can be registered and executed by the <see cref="AutomationEngine"/>.
    /// </summary>
    public interface IAutomationRule
    {
        /// <summary>
        /// Gets or sets the unique name of the rule.
        /// </summary>
        string RuleName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled for execution.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Executes the automation rule asynchronously.
        /// </summary>
        /// <param name="logger">The logger for recording execution details and errors.</param>
        /// <returns>A task that resolves to true if the rule executed successfully; otherwise, false.</returns>
        Task<bool> ExecuteAsync(Logger logger);
    }
}