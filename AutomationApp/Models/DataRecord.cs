using System.Collections.Generic;

namespace AutomationApp.Models
{
    /// <summary>
    /// Represents a single record parsed from a data file (e.g., CSV or Excel), with key-value pairs for fields.
    /// </summary>
    public class DataRecord
    {
        /// <summary>
        /// Gets or sets the dictionary of field names and their corresponding values.
        /// </summary>
        /// <remarks>Initialized with an empty <see cref="Dictionary{TKey, TValue}"/> to store field data.</remarks>
        public Dictionary<string, string> Fields { get; set; } = new();

        /// <summary>
        /// Retrieves the value of a field by its key, if it exists.
        /// </summary>
        /// <param name="key">The name of the field to retrieve.</param>
        /// <returns>The value of the field if found; otherwise, null.</returns>
        public string? GetField(string key) => Fields.TryGetValue(key, out var value) ? value : null;
    }
}