using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using System.Linq;
using AutomationApp.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace AutomationApp.Services
{
    /// <summary>
    /// A service for reading and writing data in CSV, Excel, JSON, and XML formats.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IDataService"/> to provide data operations for the automation framework.
    /// Uses <see cref="CsvHelper"/> for CSV, <see cref="ClosedXML.Excel"/> for Excel, <see cref="System.Text.Json"/> for JSON, and LINQ to XML for XML.
    /// </remarks>
    public class DataService : IDataService
    {
        private readonly Logger _logger; // Logger for recording operation details

        /// <summary>
        /// Initializes a new instance of the <see cref="DataService"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording operation details. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public DataService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Reads records from a CSV file into a list of objects.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize.</typeparam>
        /// <param name="path">The path to the CSV file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to a list of deserialized objects.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when CSV parsing fails.</exception>
        public async Task<List<T>> ReadCsvAsync<T>(string path)
        {
            // Validate input
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (!File.Exists(path))
            {
                _logger.LogInfo($"CSV file not found: {path}");
                throw new FileNotFoundException($"CSV file not found: {path}");
            }

            try
            {
                // Read and parse CSV
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
                var records = await csv.GetRecordsAsync<T>().ToListAsync();
                _logger.LogInfo($"Read {records.Count} records from CSV: {path}");
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read CSV: {path}");
                throw new InvalidOperationException($"Failed to read CSV: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a CSV or Excel file into a list of data records.
        /// </summary>
        /// <param name="filePath">The path to the file. Cannot be null or empty.</param>
        /// <param name="requiredColumns">The required columns to validate. Cannot be null.</param>
        /// <param name="logger">The logger for recording details. Cannot be null.</param>
        /// <returns>A task that resolves to a list of data records.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="requiredColumns"/> or <paramref name="logger"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="NotSupportedException">Thrown when the file extension is unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when parsing fails or no valid records are found.</exception>
        public async Task<List<DataRecord>> ParseDataFileAsync(string filePath, string[] requiredColumns, Logger logger)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
            {
                logger.LogInfo($"File not found: {filePath}");
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            if (requiredColumns == null)
                throw new ArgumentNullException(nameof(requiredColumns));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

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

        /// <summary>
        /// Writes records to a CSV file.
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize.</typeparam>
        /// <param name="path">The path to the CSV file. Cannot be null or empty.</param>
        /// <param name="data">The data to write. Cannot be null.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when writing to the file fails.</exception>
        public async Task WriteCsvAsync<T>(string path, IEnumerable<T> data)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            try
            {
                // Write CSV
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
                await csv.WriteRecordsAsync(data);
                _logger.LogInfo($"Wrote CSV to: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to write CSV: {path}");
                throw new IOException($"Failed to write CSV: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads records from an Excel file into a list of objects.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize.</typeparam>
        /// <param name="path">The path to the Excel file. Cannot be null or empty.</param>
        /// <param name="parse">The function to parse the worksheet into objects. Cannot be null.</param>
        /// <returns>A task that resolves to a list of deserialized objects.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parse"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when Excel parsing fails.</exception>
        public async Task<List<T>> ReadExcelAsync<T>(string path, Func<IXLWorksheet, List<T>> parse)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (!File.Exists(path))
            {
                _logger.LogInfo($"Excel file not found: {path}");
                throw new FileNotFoundException($"Excel file not found: {path}");
            }
            if (parse == null)
                throw new ArgumentNullException(nameof(parse));

            try
            {
                // Read Excel
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var workbook = new XLWorkbook(stream);
                var sheet = workbook.Worksheet(1);
                var records = parse(sheet);
                _logger.LogInfo($"Read {records.Count} records from Excel: {path}");
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read Excel: {path}");
                throw new InvalidOperationException($"Failed to read Excel: {ex.Message}", ex);
            }
        }

        // public async Task<List<T>> ReadExcelAsync<T>(string path) where T : new()
        // {
        //     return await ReadExcelAsync(path, sheet =>
        //     {
        //         var records = new List<T>();
        //         var headers = sheet.Row(1).CellsUsed().Select(c => c.GetString().ToLowerInvariant()).ToList();
        //         for (int row = 2; row <= sheet.LastRowUsed().RowNumber(); row++)
        //         {
        //             var record = new T();
        //             foreach (var prop in typeof(T).GetProperties())
        //             {
        //                 var colIndex = headers.IndexOf(prop.Name.ToLowerInvariant());
        //                 if (colIndex >= 0)
        //                     prop.SetValue(record, Convert.ChangeType(sheet.Cell(row, colIndex + 1).GetString(), prop.PropertyType));
        //             }
        //             records.Add(record);
        //         }
        //         return records;
        //     });
        // }

        /// <summary>
        /// Writes records to an Excel file.
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize.</typeparam>
        /// <param name="path">The path to the Excel file. Cannot be null or empty.</param>
        /// <param name="data">The data to write. Cannot be null.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when writing to the file fails.</exception>
        public async Task WriteExcelAsync<T>(string path, IEnumerable<T> data)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            try
            {
                // Write Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Data");
                worksheet.Cell(1, 1).InsertTable(data);
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await Task.Run(() => workbook.SaveAs(stream)); // ClosedXML lacks async SaveAs
                _logger.LogInfo($"Wrote Excel to: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to write Excel: {path}");
                throw new IOException($"Failed to write Excel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads a JSON file into an object.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="path">The path to the JSON file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to the deserialized object.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when JSON deserialization fails.</exception>
        public async Task<T> ReadJsonAsync<T>(string path)
        {
            // Validate input
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (!File.Exists(path))
            {
                _logger.LogInfo($"JSON file not found: {path}");
                throw new FileNotFoundException($"JSON file not found: {path}");
            }

            try
            {
                // Read JSON
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var result = await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result == null)
                {
                    _logger.LogInfo($"Failed to deserialize JSON: {path}");
                    throw new InvalidOperationException("JSON deserialization returned null.");
                }
                _logger.LogInfo($"Read JSON from: {path}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read JSON: {path}");
                throw new InvalidOperationException($"Failed to read JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Writes an object to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="path">The path to the JSON file. Cannot be null or empty.</param>
        /// <param name="data">The data to write. Cannot be null.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when writing to the file fails.</exception>
        public async Task WriteJsonAsync<T>(string path, T data)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            try
            {
                // Write JSON
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInfo($"Wrote JSON to: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to write JSON: {path}");
                throw new IOException($"Failed to write JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads an XML file into an XDocument.
        /// </summary>
        /// <param name="path">The path to the XML file. Cannot be null or empty.</param>
        /// <returns>A task that resolves to the XDocument.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when XML parsing fails.</exception>
        public async Task<XDocument> ReadXmlAsync(string path)
        {
            // Validate input
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (!File.Exists(path))
            {
                _logger.LogInfo($"XML file not found: {path}");
                throw new FileNotFoundException($"XML file not found: {path}");
            }

            try
            {
                // Read XML
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var doc = await Task.Run(() => XDocument.Load(stream)); // XDocument.Load lacks async
                _logger.LogInfo($"Read XML from: {path}");
                return doc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read XML: {path}");
                throw new InvalidOperationException($"Failed to read XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Writes an XDocument to an XML file.
        /// </summary>
        /// <param name="path">The path to the XML file. Cannot be null or empty.</param>
        /// <param name="doc">The XDocument to write. Cannot be null.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="doc"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when writing to the file fails.</exception>
        public async Task WriteXmlAsync(string path, XDocument doc)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            try
            {
                // Write XML
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await Task.Run(() => doc.Save(stream)); // XDocument.Save lacks async
                _logger.LogInfo($"Wrote XML to: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to write XML: {path}");
                throw new IOException($"Failed to write XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a CSV file into a list of data records asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <param name="requiredColumns">The required columns to validate.</param>
        /// <param name="logger">The logger for recording details.</param>
        /// <returns>A task that resolves to a list of data records.</returns>
        /// <exception cref="InvalidOperationException">Thrown when required columns are missing or no valid records are found.</exception>
        private async Task<List<DataRecord>> ParseCsvAsync(string filePath, string[] requiredColumns, Logger logger)
        {
            // Configure CSV reader
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            var records = new List<DataRecord>();
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord?.Select(h => h.ToLowerInvariant()).ToList() ?? [];

            // Validate required columns
            foreach (var col in requiredColumns)
            {
                if (!headers.Contains(col.ToLowerInvariant()))
                {
                    logger.LogInfo($"Required column '{col}' not found in CSV: {filePath}");
                    throw new InvalidOperationException($"Required column '{col}' not found");
                }
            }

            // Parse records
            while (await csv.ReadAsync())
            {
                try
                {
                    var record = new DataRecord();
                    foreach (var header in headers)
                    {
                        record.Fields[header] = csv.GetField(header) ?? string.Empty;
                    }

                    bool isValid = true;
                    foreach (var col in requiredColumns)
                    {
                        if (string.IsNullOrWhiteSpace(record.GetField(col)))
                        {
                            logger.LogWarning($"Missing required field '{col}' in row {csv.Context.Parser.Row} of {filePath}");
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                        records.Add(record);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Skipping invalid row {csv.Context.Parser.Row} in {filePath}");
                }
            }

            if (!records.Any())
            {
                logger.LogInfo($"No valid records found in CSV: {filePath}");
                throw new InvalidOperationException("No valid records found in CSV file");
            }

            logger.LogInfo($"Parsed {records.Count} valid records from CSV: {filePath}");
            return records;
        }

        /// <summary>
        /// Parses an Excel file into a list of data records asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the Excel file.</param>
        /// <param name="requiredColumns">The required columns to validate.</param>
        /// <param name="logger">The logger for recording details.</param>
        /// <returns>A task that resolves to a list of data records.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the file is empty, required columns are missing, or no valid records are found.</exception>
        private async Task<List<DataRecord>> ParseExcelAsync(string filePath, string[] requiredColumns, Logger logger)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var records = new List<DataRecord>();

            if (worksheet.IsEmpty())
            {
                logger.LogInfo($"Excel file is empty: {filePath}");
                throw new InvalidOperationException("Excel file is empty");
            }

            // Read headers
            var headers = new Dictionary<string, int>();
            for (int col = 1; col <= worksheet.LastColumnUsed().ColumnNumber(); col++)
            {
                var header = worksheet.Cell(1, col).GetString()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(header))
                    headers[header] = col;
            }

            // Validate required columns
            foreach (var col in requiredColumns)
            {
                if (!headers.ContainsKey(col.ToLowerInvariant()))
                {
                    logger.LogInfo($"Required column '{col}' not found in Excel: {filePath}");
                    throw new InvalidOperationException($"Required column '{col}' not found");
                }
            }

            // Parse records
            for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
            {
                try
                {
                    var record = new DataRecord();
                    foreach (var header in headers)
                    {
                        record.Fields[header.Key] = worksheet.Cell(row, header.Value).GetString() ?? string.Empty;
                    }

                    bool isValid = true;
                    foreach (var col in requiredColumns)
                    {
                        if (string.IsNullOrWhiteSpace(record.GetField(col)))
                        {
                            logger.LogWarning($"Missing required field '{col}' in row {row} of {filePath}");
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                        records.Add(record);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Skipping invalid row {row} in {filePath}");
                }
            }

            if (!records.Any())
            {
                logger.LogInfo($"No valid records found in Excel: {filePath}");
                throw new InvalidOperationException("No valid records found in Excel file");
            }

            logger.LogInfo($"Parsed {records.Count} valid records from Excel: {filePath}");
            return records;
        }

        // public async IAsyncEnumerable<DataRecord> ParseCsvStreamAsync(string filePath, string[] requiredColumns, Logger logger)
        // {
        //     using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        //     using var reader = new StreamReader(stream);
        //     using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
        //     await csv.ReadAsync();
        //     csv.ReadHeader();
        //     while (await csv.ReadAsync())
        //     {
        //         var record = new DataRecord();
        //         foreach (var header in csv.HeaderRecord)
        //             record.Fields[header] = csv.GetField(header) ?? string.Empty;
        //         yield return record;
        //     }
        // }
    }
}