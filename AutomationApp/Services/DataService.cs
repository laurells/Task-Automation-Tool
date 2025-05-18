using System.Text.Json;
using System.Xml.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Globalization;
using AutomationApp.Models;
using OfficeOpenXml;

namespace AutomationApp.Services
{
    /// <summary>
    /// A service for reading and writing data in various formats including CSV, Excel, JSON, and XML.
    /// </summary>
    /// <remarks>
    /// This class provides methods to read and write data in different formats.
    /// It uses CsvHelper for CSV, ClosedXML for Excel, System.Text.Json for JSON, and LINQ to XML for XML.
    /// </remarks>
    /// <example>
    /// <code>
    /// var data = DataService.ReadCsv<MyClass>("data.csv");
    /// DataService.WriteJson("data.json", data);
    /// var xmlDoc = DataService.ReadXml("data.xml");
    /// DataService.WriteXml("output.xml", xmlDoc);
    /// </code>
    /// </example>
    public class DataService
    {
        private readonly Logger _logger;
        public DataService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // CSV -> List<T>
        public List<T> ReadCsv<T>(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Console.WriteLine($"CSV file not found: {path}");
                return new List<T>();
            }

            try
            {
                using var reader = new StreamReader(path);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
                return new List<T>(csv.GetRecords<T>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV {path}: {ex.Message}");
                return new List<T>();
            }
        }
        public async Task<List<DataRecord>> ParseDataFile(string filePath, string[] requiredColumns, Logger logger)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                logger.LogInfo($"File not found: {filePath}");
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            try
            {
                if (extension == ".csv")
                {
                    return await ParseCsvAsync(filePath, requiredColumns, logger);
                }
                else if (extension == ".xlsx" || extension == ".xls")
                {
                    return await ParseExcelAsync(filePath, requiredColumns, logger);
                }
                else
                {
                    logger.LogInfo($"Unsupported file type: {extension}");
                    throw new NotSupportedException($"Unsupported file type: {extension}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to parse file: {filePath}");
                throw;
            }
        }

        private async Task<List<DataRecord>> ParseCsvAsync(string filePath, string[] requiredColumns, Logger logger)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            var records = new List<DataRecord>();
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord?.Select(h => h.ToLowerInvariant()).ToList() ?? [];

            foreach (var col in requiredColumns)
            {
                if (!headers.Contains(col.ToLowerInvariant()))
                {
                    logger.LogInfo($"Required column '{col}' not found in CSV: {filePath}");
                    throw new InvalidOperationException($"Required column '{col}' not found");
                }
            }

            while (await csv.ReadAsync())
            {
                try
                {
                    var record = new DataRecord();
                    foreach (var header in headers)
                    {
                        record.Fields[header] = csv.GetField(header) ?? string.Empty;
                    }

                    foreach (var col in requiredColumns)
                    {

                        if (string.IsNullOrWhiteSpace(record.GetField(col)))
                        {

                            logger.LogWarning($"Missing required field '{col}' in row {csv.Context.Parser.Row}");

                            continue;
                        }
                        
                    }

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Skipping invalid row {csv.Context.Parser.Row} in {filePath}");
                }
            }

            if (!records.Any())
            {
                logger.LogInfo($"No valid records found in {filePath}");
                throw new InvalidOperationException("No valid records found in CSV file");
            }

            logger.LogInfo($"Parsed {records.Count} valid records from {filePath}");
            return records;
        }

        private async Task<List<DataRecord>> ParseExcelAsync(string filePath, string[] requiredColumns, Logger logger)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var package = new ExcelPackage();
            await package.LoadAsync(stream); // Async load
            var worksheet = package.Workbook.Worksheets[0];
            var records = new List<DataRecord>();

            if (worksheet.Dimension == null)
            {
                logger.LogInfo($"Excel file is empty: {filePath}");
                throw new InvalidOperationException("Excel file is empty");
            }

            var headers = new Dictionary<string, int>();
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[1, col].Text?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(header))
                {
                    headers[header] = col;
                }
            }

            foreach (var col in requiredColumns)
            {
                if (!headers.ContainsKey(col.ToLowerInvariant()))
                {
                    logger.LogInfo($"Required column '{col}' not found in Excel: {filePath}");
                    throw new InvalidOperationException($"Required column '{col}' not found");
                }
            }

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                try
                {
                    var record = new DataRecord();
                    foreach (var header in headers)
                    {
                        record.Fields[header.Key] = worksheet.Cells[row, header.Value].Text ?? string.Empty;
                    }

                    bool isValid = true;
                    foreach (var col in requiredColumns)
                    {
                        if (string.IsNullOrWhiteSpace(record.GetField(col)))
                        {
                            logger.LogWarning($"Missing required field '{col}' in row {row}");
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        records.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Skipping invalid row {row} in {filePath}");
                }
            }

            if (!records.Any())
            {
                _logger.LogInfo($"No valid records found in {filePath}");
                throw new InvalidOperationException("No valid records found in Excel file");
            }

            logger.LogInfo($"Parsed {records.Count} valid records from {filePath}");
            return records;
        }
    
        // List<T> -> CSV
        public static void WriteCsv<T>(string path, IEnumerable<T> data)
        {
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
            csv.WriteRecords(data);
            Console.WriteLine($"CSV written to {path}");
        }

        // Excel -> List<T>
        public static List<T> ReadExcel<T>(string path, Func<IXLWorksheet, List<T>> parse)
        {
            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet(1);
            return parse(sheet);
        }

        // List<T> -> Excel
        public static void WriteExcel<T>(string path, IEnumerable<T> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");
            worksheet.Cell(1, 1).InsertTable(data);
            workbook.SaveAs(path);
            Console.WriteLine($"Excel file written to {path}");
        }

        // JSON -> T
        public static T ReadJson<T>(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json)!;
        }

        // T -> JSON
        public static void WriteJson<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Console.WriteLine($"JSON file written to {path}");
        }

        // XML -> XDocument
        public static XDocument ReadXml(string path)
        {
            return XDocument.Load(path);
        }

        // XDocument -> XML
        public static void WriteXml(string path, XDocument doc)
        {
            doc.Save(path);
            Console.WriteLine($"XML file written to {path}");
        }
    }
}
