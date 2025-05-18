namespace AutomationApp.Models
{
    public class DataRecord
    {
        public Dictionary<string, string> Fields { get; set; } = new();

        public string? GetField(string key) => Fields.TryGetValue(key, out var value) ? value : null;
    }
}