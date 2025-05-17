using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using AutomationApp.Core;
using AutomationApp.Interfaces;

namespace AutomationApp.Gui.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public ObservableCollection<IAutomationRule> Rules { get; } = new();
        private readonly AutomationEngine _engine;

        public ReactiveCommand<string, Unit> RunCommand { get; }
        public ReactiveCommand<Unit, Unit> RunAllCommand { get; }

        public MainWindowViewModel()
        {
            _engine = RuleConfigLoader.LoadRules(); // Load from JSON
            foreach (var rule in _engine.GetRules()) Rules.Add(rule);

            RunCommand = ReactiveCommand.CreateFromTask<string>(async ruleName =>
            {
                var rule = Rules.FirstOrDefault(r => r.RuleName == ruleName);
                if (rule != null) await rule.ExecuteAsync();
            });

            RunAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _engine.ExecuteAllAsync();
            });
        }
    }
}
