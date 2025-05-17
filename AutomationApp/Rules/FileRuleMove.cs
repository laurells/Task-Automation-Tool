using AutomationApp.Interfaces;
using AutomationApp.Services;

public class FileMoveRule(string source, string target, FileService fileService) : IAutomationRule
{
    public string RuleName => "MovePDFsToSorted";
    private readonly string _source = source;
    private readonly string _target = target;
    private readonly FileService _fileService = fileService;

    public async Task ExecuteAsync()
    {
        var files = Directory.GetFiles(_source, "*.pdf");
        foreach (var file in files)
        {
            var dest = Path.Combine(_target, Path.GetFileName(file));
            FileService.MoveFile(file, dest);
        }

        await Task.CompletedTask;
    }
}
