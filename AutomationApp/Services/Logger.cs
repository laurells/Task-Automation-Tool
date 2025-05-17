public static class Logger
{
    public static void Log(string message)
    {
        File.AppendAllText("automation.log", $"{DateTime.Now}: {message}{Environment.NewLine}");
    }

    public static void LogError(Exception ex)
    {
        File.AppendAllText("automation.log", $"{DateTime.Now} ERROR: {ex.Message}{Environment.NewLine}");
    }
}
