using AutomationApp.Services;
using AutomationApp.Interfaces;

namespace AutomationApp.Rules
{
    /// <summary>
    /// Represents an automation rule for processing data records from a CSV or Excel file.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IAutomationRule"/> to integrate with the automation engine.
    /// Parses data using <see cref="DataService"/> and logs record details using the provided logger.
    /// </remarks>
    public class DataProcessingRule : IAutomationRule
    {
        private readonly IDataService _dataService;   // Service for parsing data files
        private readonly string _filePath;           // Path to the data file
        private readonly string[] _requiredColumns;  // Required columns for validation
        private readonly Logger _logger;             // Logger for execution details
        private string _name;                        // Backing field for RuleName

        /// <summary>
        /// Initializes a new instance of the <see cref="DataProcessingRule"/> class.
        /// </summary>
        /// <param name="dataService">The service for parsing data files. Cannot be null.</param>
        /// <param name="filePath">The path to the data file (CSV or Excel). Cannot be null.</param>
        /// <param name="requiredColumns">The required columns to validate in the data file. Cannot be null.</param>
        /// <param name="logger">The logger for recording execution details. Cannot be null.</param>
        /// <param name="name">The name of the rule. Defaults to "DataProcessingRule" if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public DataProcessingRule(IDataService dataService, string filePath, string[] requiredColumns, Logger logger, string name = "DataProcessingRule")
        {
            // Validate dependencies
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _requiredColumns = requiredColumns ?? throw new ArgumentNullException(nameof(requiredColumns));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets or sets the unique name of the rule.
        /// </summary>
        /// <remarks>Defaults to "DataProcessingRule" if not specified in the constructor.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when setting to null.</exception>
        public string RuleName
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled for execution.
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Executes the data processing rule by parsing records from a data file and logging their fields.
        /// </summary>
        /// <param name="logger">The logger for recording execution details and errors. Cannot be null.</param>
        /// <returns>A task that resolves to true if the records were processed successfully; otherwise, false.</returns>
        /// <remarks>
        /// Parses data using <see cref="DataService.ParseDataFile"/> and logs each record’s fields.
        /// Returns false if the rule is disabled, the file is empty, or an error occurs.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public async Task<bool> ExecuteAsync(Logger? logger = null)
        {
            var log = logger ?? _logger;
            log.LogInfo($"Executing DataProcessingRule '{RuleName}' with file: {_filePath}");

            try
            {
                var records = await _dataService.ParseDataFileAsync(_filePath, _requiredColumns, (Logger)log);
                if (records == null || !records.Any())
                {
                    log.LogWarning($"No valid records found in file: {_filePath}");
                    return false;
                }

                // Log each record
                foreach (var record in records)
                {
                    var rowData = string.Join(", ", record.Fields.Select(kv => $"{kv.Key}: {kv.Value}"));
                    log.LogInfo($"Record: {rowData}");
                }

                return true;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to execute DataProcessingRule '{RuleName}'");
                return false;
            }
        }
    }
}