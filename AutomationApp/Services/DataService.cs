using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Globalization;

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

        // CSV -> List<T>
        public static List<T> ReadCsv<T>(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            return new List<T>(csv.GetRecords<T>());
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
