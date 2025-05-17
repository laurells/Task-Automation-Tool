using System;
using System.IO;
using System.IO.Compression;

namespace AutomationApp.Services
{
    public class FileService
    {
        public static void CopyFile(string source, string destination)
        {
            File.Copy(source, destination, overwrite: true);
            Console.WriteLine($"Copied: {source} -> {destination}");
        }

        public static void MoveFile(string source, string destination)
        {
            File.Move(source, destination, overwrite: true);
            Console.WriteLine($"Moved: {source} -> {destination}");
        }

        public static void DeleteFile(string filePath)
        {
            File.Delete(filePath);
            Console.WriteLine($"Deleted: {filePath}");
        }

        public static void RenameFile(string source, string newName)
        {
            var directory = Path.GetDirectoryName(source);
            var newPath = Path.Combine(directory!, newName);
            File.Move(source, newPath);
            Console.WriteLine($"Renamed: {source} -> {newPath}");
        }

        public static void ZipFile(string source, string zipPath)
        {
            using var archive = System.IO.Compression.ZipFile.Open(zipPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(source, Path.GetFileName(source));
            Console.WriteLine($"Archived: {source} -> {zipPath}");
        }

        public static void UnzipFile(string zipPath, string extractTo)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractTo, overwriteFiles: true);
            Console.WriteLine($"Unzipped: {zipPath} -> {extractTo}");
        }
    }
}
