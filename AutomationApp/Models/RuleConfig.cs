using System.Text.Json.Serialization;

namespace AutomationApp.Models
{
    /// <summary>
    /// Represents a rule configuration loaded from JSON.
    /// </summary>
    public class RuleConfig
    {
        /// <summary>
        /// Gets or sets the type of the rule (e.g., "filemoverule", "bulkemailrule").
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the rule.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the source path for FileMoveRule.
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the target path for FileMoveRule.
        /// </summary>
        [JsonPropertyName("target")]
        public string? Target { get; set; }

        /// <summary>
        /// Gets or sets the supported file extensions for FileMoveRule.
        /// </summary>
        [JsonPropertyName("supportedExtensions")]
        public string[]? SupportedExtensions { get; set; }

        /// <summary>
        /// Gets or sets whether to add a timestamp to moved files for FileMoveRule.
        /// </summary>
        [JsonPropertyName("addTimestamp")]
        public bool AddTimestamp { get; set; }

        /// <summary>
        /// Gets or sets whether to backup files for FileMoveRule.
        /// </summary>
        [JsonPropertyName("backupFiles")]
        public bool BackupFiles { get; set; }

        /// <summary>
        /// Gets or sets the CSV file path for BulkEmailRule.
        /// </summary>
        [JsonPropertyName("csvPath")]
        public string? CsvPath { get; set; }

        /// <summary>
        /// Gets or sets the data file path for DataProcessingRule.
        /// </summary>
        [JsonPropertyName("filePath")]
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the required columns for DataProcessingRule.
        /// </summary>
        [JsonPropertyName("requiredColumns")]
        public string[]? RequiredColumns { get; set; }

        /// <summary>
        /// Gets or sets the nested settings object for rules that use it.
        /// </summary>
        [JsonPropertyName("settings")]
        public RuleSettings? Settings { get; set; }
    }

    /// <summary>
    /// Represents nested settings for rules that use them.
    /// </summary>
    public class RuleSettings
    {
        /// <summary>
        /// Gets or sets the source path for the rule.
        /// </summary>
        [JsonPropertyName("sourcePath")]
        public string? SourcePath { get; set; }

        /// <summary>
        /// Gets or sets the target path for the rule.
        /// </summary>
        [JsonPropertyName("targetPath")]
        public string? TargetPath { get; set; }

        /// <summary>
        /// Gets or sets the supported file extensions for the rule.
        /// </summary>
        [JsonPropertyName("supportedExtensions")]
        public string[]? SupportedExtensions { get; set; }

        /// <summary>
        /// Gets or sets whether to add a timestamp to moved files.
        /// </summary>
        [JsonPropertyName("addTimestamp")]
        public bool AddTimestamp { get; set; }

        /// <summary>
        /// Gets or sets whether to backup files.
        /// </summary>
        [JsonPropertyName("backupFiles")]
        public bool BackupFiles { get; set; }

        /// <summary>
        /// Gets or sets the data file path for DataProcessingRule.
        /// </summary>
        [JsonPropertyName("dataPath")]
        public string? DataPath { get; set; }

        /// <summary>
        /// Gets or sets the required columns for DataProcessingRule.
        /// </summary>
        [JsonPropertyName("requiredColumns")]
        public string[]? RequiredColumns { get; set; }
    }
}