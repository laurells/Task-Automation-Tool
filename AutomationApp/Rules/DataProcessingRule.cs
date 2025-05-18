using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp.Rules
{
    public class DataProcessingRule : IAutomationRule
    {
        private readonly DataService _dataService;
        private readonly string _filePath;
        private readonly string[] _requiredColumns;
        private readonly Logger _logger;
        private readonly string _name;


        public DataProcessingRule(DataService dataService, string filePath, string[] requiredColumns, Logger logger, string name = "DataProcessingRule")
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _requiredColumns = requiredColumns ?? throw new ArgumentNullException(nameof(requiredColumns));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string RuleName => _name;
        public bool Enabled { get; set; } = true;

        public async Task<bool> ExecuteAsync()
        {
            if (!Enabled)
            {
                _logger.LogInfo($"Rule '{RuleName}' is disabled. Skipping execution.");
                return false;
            }

            _logger.LogInfo($"Executing DataProcessingRule '{RuleName}' with file: {_filePath}");

            try
            {
                var records = await _dataService.ParseDataFile(_filePath, _requiredColumns, _logger);
                if (records == null || records.Count == 0)
                {
                    _logger.LogWarning($"No records found in file: {_filePath}");
                    return false;
                }
                foreach (var record in records)
                {
                    var fields = string.Join(", ", record.Fields.Select(f => $"{f.Key}: {f.Value}"));
                    _logger.LogInfo($"Processed record: {fields}");
                }

                _logger.LogInfo($"DataProcessingRule '{RuleName}' completed. Processed {records.Count} records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to execute DataProcessingRule '{RuleName}'");
                throw;
            }
            return true;
        }
    }
}