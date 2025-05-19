using AutomationApp.Models;
using System.Xml.Linq;
using ClosedXML.Excel;


namespace AutomationApp.Services
{
    /// <summary>
    /// Defines the contract for data reading and writing operations in various formats.
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Reads records from a CSV file into a list of objects.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize.</typeparam>
        /// <param name="path">The path to the CSV file.</param>
        /// <returns>A task that resolves to a list of deserialized objects.</returns>
        Task<List<T>> ReadCsvAsync<T>(string path);

        /// <summary>
        /// Parses a CSV or Excel file into a list of data records.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="requiredColumns">The required columns to validate.</param>
        /// <param name="logger">The logger for recording details.</param>
        /// <returns>A task that resolves to a list of data records.</returns>
        Task<List<DataRecord>> ParseDataFileAsync(string filePath, string[] requiredColumns, Logger logger);

        /// <summary>
        /// Writes records to a CSV file.
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize.</typeparam>
        /// <param name="path">The path to the CSV file.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteCsvAsync<T>(string path, IEnumerable<T> data);

        /// <summary>
        /// Reads records from an Excel file into a list of objects.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize.</typeparam>
        /// <param name="path">The path to the Excel file.</param>
        /// <param name="parse">The function to parse the worksheet into objects.</param>
        /// <returns>A task that resolves to a list of deserialized objects.</returns>
        Task<List<T>> ReadExcelAsync<T>(string path, Func<IXLWorksheet, List<T>> parse);

        /// <summary>
        /// Writes records to an Excel file.
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize.</typeparam>
        /// <param name="path">The path to the Excel file.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteExcelAsync<T>(string path, IEnumerable<T> data);

        /// <summary>
        /// Reads a JSON file into an object.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="path">The path to the JSON file.</param>
        /// <returns>A task that resolves to the deserialized object.</returns>
        Task<T> ReadJsonAsync<T>(string path);

        /// <summary>
        /// Writes an object to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="path">The path to the JSON file.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteJsonAsync<T>(string path, T data);

        /// <summary>
        /// Reads an XML file into an XDocument.
        /// </summary>
        /// <param name="path">The path to the XML file.</param>
        /// <returns>A task that resolves to the XDocument.</returns>
        Task<XDocument> ReadXmlAsync(string path);

        /// <summary>
        /// Writes an XDocument to an XML file.
        /// </summary>
        /// <param name="path">The path to the XML file.</param>
        /// <param name="doc">The XDocument to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteXmlAsync(string path, XDocument doc);
        
    }
}