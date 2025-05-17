using AutomationApp;

public class CommandHandler
{
    public static async Task HandleAsync(string[] args, AutomationEngine engine)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Commands: run | schedule | help");
            return;
        }

        switch (args[0])
        {
            case "run":
                await engine.ExecuteAllAsync();
                break;
            case "schedule":
                var scheduler = new AutomationScheduler(engine, 30); // run every 30 sec
                scheduler.Start();
                Console.ReadLine();
                scheduler.Stop();
                break;
            case "help":
            default:
                Console.WriteLine("Usage: automation.exe run | schedule");
                break;
        }
    }
}
